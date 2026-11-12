// The audio engine of the player: two decks (audio elements) that take turns, so the next track is
// loaded while the current one plays and starts the moment it ends (gapless as far as the browser
// allows). With Web Audio, each deck runs through its own gain (ReplayGain) into a master gain
// (volume) and an analyser, which later visualizations read; without Web Audio, the volume of the
// elements does both. Transcoded tracks arrive as HLS and are attached through hls.js. The engine
// knows nothing about the queue: the player store decides what plays.

import { browserHlsSupport, type HlsAttachment, type HlsSupport } from './hls'

export interface DeckSource {
    /** URL of the stream. */
    readonly url: string
    /** Gain factor of the track (ReplayGain), 1 for none. */
    readonly gain: number
    /** Whether the URL is an HLS master playlist instead of a file. */
    readonly hls?: boolean
}

export interface EngineEvents {
    /** Position and duration in seconds; the duration is 0 while unknown. */
    timeupdate?(position: number, duration: number): void
    playing?(): void
    paused?(): void
    /** The playback waits for data. */
    waiting?(): void
    /** The current track ended and no next one was prepared. */
    ended?(): void
    /** The prepared track took over from the current one, which ended. */
    advanced?(): void
    /** The current track failed to load or play. */
    error?(error: unknown): void
}

/** What the engine needs from the browser; tests replace it. */
export interface AudioEnvironment {
    createAudio(): HTMLAudioElement
    /** A new audio context, or null without Web Audio. */
    createContext(): AudioContext | null
    setTimeout(callback: () => void, delay: number): number
    clearTimeout(handle: number): void
    /** HLS playback; the browser's by default. */
    hls?: HlsSupport
}

export const browserAudioEnvironment: AudioEnvironment = {
    createAudio: () => {
        const audio = new Audio()
        audio.preload = 'auto'
        return audio
    },
    createContext: () => {
        const Context =
            typeof window === 'undefined'
                ? undefined
                : (window.AudioContext ??
                  (window as unknown as { webkitAudioContext?: typeof AudioContext })
                      .webkitAudioContext)
        try {
            return Context ? new Context({ latencyHint: 'playback' }) : null
        } catch {
            return null
        }
    },
    setTimeout: (callback, delay) => window.setTimeout(callback, delay),
    clearTimeout: (handle) => window.clearTimeout(handle),
    hls: browserHlsSupport,
}

/**
 * The next track starts this many seconds before the current one ends, which roughly makes up for
 * the time a starting element needs until it sounds.
 */
export const handoverLead = 0.05

/** Seconds before the end of a track from which the start of the next one is timed. */
export const handoverWindow = 0.6

interface Deck {
    readonly audio: HTMLAudioElement
    gain: GainNode | null
    source: DeckSource | null
    /** The attached HLS stream, if the source is one. */
    hls: HlsAttachment | null
    /** Increases with every new source, so a late HLS attachment of an older one is dropped. */
    loads: number
}

function finiteOrZero(value: number): number {
    return Number.isFinite(value) && value > 0 ? value : 0
}

export class AudioEngine {
    private readonly decks: [Deck, Deck]
    private current = 0
    private prepared: DeckSource | null = null
    private context: AudioContext | null = null
    private master: GainNode | null = null
    private analyserNode: AnalyserNode | null = null
    private graphReady = false
    /** Whether the last request was play; a pause while the context resumes cancels it. */
    private wantsPlay = false
    private volume = 1
    private handover: number | null = null

    constructor(
        private readonly events: EngineEvents = {},
        private readonly environment: AudioEnvironment = browserAudioEnvironment,
    ) {
        this.decks = [this.createDeck(0), this.createDeck(1)]
    }

    /** The analyser of the output; null without Web Audio or before the first playback. */
    get analyser(): AnalyserNode | null {
        return this.analyserNode
    }

    /** The source of the current deck. */
    get source(): DeckSource | null {
        return this.deck.source
    }

    get position(): number {
        return finiteOrZero(this.deck.audio.currentTime)
    }

    get duration(): number {
        return finiteOrZero(this.deck.audio.duration)
    }

    get paused(): boolean {
        return this.deck.audio.paused
    }

    /** Whether a next track is loaded on the other deck. */
    get hasPrepared(): boolean {
        return this.prepared !== null
    }

    /** Loads a track on the current deck, optionally at a position, and starts it with play. */
    load(source: DeckSource, position = 0): void {
        this.cancelHandover()
        this.prepare(null)
        const deck = this.deck
        deck.source = source
        this.applyGain(deck)
        this.attach(deck, source)
        if (position > 0) {
            const audio = deck.audio
            const seek = (): void => {
                audio.currentTime = position
            }
            audio.addEventListener('loadedmetadata', seek, { once: true })
        }
    }

    /** Loads the next track on the other deck, or forgets it with null. */
    prepare(source: DeckSource | null): void {
        const other = this.other
        if (source && source.url === this.prepared?.url && source.gain === this.prepared.gain) {
            return
        }

        this.prepared = source
        if (source) {
            other.source = source
            this.applyGain(other)
            this.attach(other, source)
        } else {
            this.unload(other)
        }
    }

    async play(): Promise<void> {
        this.wantsPlay = true
        this.ensureGraph()
        if (this.context?.state === 'suspended') {
            await this.context.resume().catch(() => undefined)
            // Paused or stopped while the context resumed: stay paused.
            if (!this.wantsPlay) {
                return
            }
        }

        try {
            await this.deck.audio.play()
        } catch (error) {
            // An interrupted play (pause or new source meanwhile) is no error.
            if (!(error instanceof DOMException && error.name === 'AbortError')) {
                this.events.error?.(error)
            }
        }
    }

    pause(): void {
        this.wantsPlay = false
        this.cancelHandover()
        this.deck.audio.pause()
    }

    /** Stops and unloads both decks. */
    stop(): void {
        this.wantsPlay = false
        this.cancelHandover()
        this.prepare(null)
        this.unload(this.deck)
    }

    seek(position: number): void {
        this.cancelHandover()
        const audio = this.deck.audio
        const duration = finiteOrZero(audio.duration)
        audio.currentTime = Math.max(0, duration > 0 ? Math.min(position, duration) : position)
    }

    /** Output gain between 0 and 1 (already shaped by the volume curve). */
    setVolume(gain: number): void {
        this.volume = gain
        if (this.master) {
            this.master.gain.value = gain
        }

        this.decks.forEach((deck) => this.applyGain(deck))
    }

    /** Changes the gain of the current track, e.g. when the ReplayGain mode changes. */
    setTrackGain(gain: number): void {
        const deck = this.deck
        if (deck.source) {
            deck.source = { ...deck.source, gain }
            this.applyGain(deck)
        }
    }

    private get deck(): Deck {
        return this.decks[this.current]!
    }

    private get other(): Deck {
        return this.decks[1 - this.current]!
    }

    private createDeck(index: number): Deck {
        const audio = this.environment.createAudio()
        const deck: Deck = { audio, gain: null, source: null, hls: null, loads: 0 }
        const active = (): boolean => this.decks[this.current] === deck
        audio.addEventListener('timeupdate', () => {
            if (active()) {
                this.events.timeupdate?.(
                    finiteOrZero(audio.currentTime),
                    finiteOrZero(audio.duration),
                )
                this.scheduleHandover()
            }
        })
        audio.addEventListener('durationchange', () => {
            if (active()) {
                this.events.timeupdate?.(
                    finiteOrZero(audio.currentTime),
                    finiteOrZero(audio.duration),
                )
            }
        })
        audio.addEventListener('playing', () => active() && this.events.playing?.())
        audio.addEventListener('pause', () => {
            if (active() && !audio.ended) {
                this.events.paused?.()
            }
        })
        audio.addEventListener('waiting', () => active() && this.events.waiting?.())
        audio.addEventListener('ended', () => active() && this.onEnded())
        audio.addEventListener('error', () => {
            if (active() && deck.source) {
                this.events.error?.(audio.error ?? new Error(`Deck ${index} failed to play.`))
            }
        })
        return deck
    }

    /** Builds the Web Audio graph once, within the first play (a user gesture). */
    private ensureGraph(): void {
        if (this.graphReady) {
            return
        }

        this.graphReady = true
        const context = this.environment.createContext()
        if (!context) {
            return
        }

        try {
            const master = context.createGain()
            const analyser = context.createAnalyser()
            master.connect(analyser)
            analyser.connect(context.destination)
            for (const deck of this.decks) {
                const gain = context.createGain()
                context.createMediaElementSource(deck.audio).connect(gain)
                gain.connect(master)
                deck.gain = gain
            }

            master.gain.value = this.volume
            this.context = context
            this.master = master
            this.analyserNode = analyser
        } catch {
            // Without the graph the elements play on their own.
            this.decks.forEach((deck) => (deck.gain = null))
            void context.close().catch(() => undefined)
        }

        this.decks.forEach((deck) => this.applyGain(deck))
    }

    private applyGain(deck: Deck): void {
        const trackGain = deck.source?.gain ?? 1
        if (deck.gain && this.master) {
            deck.gain.gain.value = trackGain
            deck.audio.volume = 1
        } else {
            deck.audio.volume = Math.min(1, Math.max(0, this.volume * trackGain))
        }
    }

    /** Starts the prepared deck shortly before the current one ends. */
    private scheduleHandover(): void {
        if (!this.prepared || this.handover !== null || this.deck.audio.paused) {
            return
        }

        const audio = this.deck.audio
        const remaining = finiteOrZero(audio.duration) - audio.currentTime
        if (finiteOrZero(audio.duration) === 0 || remaining > handoverWindow) {
            return
        }

        const delay = Math.max(0, (remaining - handoverLead) * 1000)
        this.handover = this.environment.setTimeout(() => {
            this.handover = null
            this.takeOver(false)
        }, delay)
    }

    private cancelHandover(): void {
        if (this.handover !== null) {
            this.environment.clearTimeout(this.handover)
            this.handover = null
        }
    }

    private onEnded(): void {
        this.cancelHandover()
        if (this.prepared) {
            this.takeOver(true)
        } else {
            this.events.ended?.()
        }
    }

    /**
     * The prepared deck becomes the current one and plays. The old one is unloaded at once if it
     * ended, otherwise once its last moments have played, unless it was prepared again meanwhile.
     */
    private takeOver(ended: boolean): void {
        if (!this.prepared) {
            return
        }

        const previous = this.deck
        this.current = 1 - this.current
        this.prepared = null
        const audio = this.deck.audio
        void audio.play().catch((error: unknown) => this.events.error?.(error))
        previous.source = null
        if (ended) {
            this.unload(previous)
        } else {
            this.environment.setTimeout(() => {
                if (previous !== this.deck && previous.source === null) {
                    this.unload(previous)
                }
            }, 1000)
        }

        this.events.advanced?.()
        this.events.timeupdate?.(finiteOrZero(audio.currentTime), finiteOrZero(audio.duration))
    }

    /** Sets the source of a deck: a file directly, an HLS stream through the HLS support. */
    private attach(deck: Deck, source: DeckSource): void {
        this.detachHls(deck)
        const load = ++deck.loads
        if (!source.hls) {
            deck.audio.src = source.url
            deck.audio.load()
            return
        }

        const failed = (error: unknown): void => {
            if (deck.loads === load && this.decks[this.current] === deck) {
                this.events.error?.(error)
            }
        }
        const support = this.environment.hls ?? browserHlsSupport
        support
            .attach(deck.audio, source.url, failed)
            .then((attachment) => {
                if (deck.loads === load) {
                    deck.hls = attachment
                } else {
                    attachment.destroy()
                }
            })
            .catch(failed)
    }

    private detachHls(deck: Deck): void {
        deck.loads++
        const attachment = deck.hls
        deck.hls = null
        attachment?.destroy()
    }

    private unload(deck: Deck): void {
        deck.source = null
        this.detachHls(deck)
        if (deck.audio.getAttribute('src') !== null) {
            deck.audio.pause()
            deck.audio.removeAttribute('src')
            deck.audio.load()
        }
    }
}
