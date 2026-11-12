// The audio engine of the player: two decks (audio elements) that take turns, so the next track is
// loaded while the current one plays and starts the moment it ends (gapless as far as the browser
// allows), or fades into it with a crossfade. With Web Audio, each deck runs through its own gain
// (ReplayGain) and fade (crossfade) into the equalizer, the output fade (sleep timer), the master gain
// (volume) and an analyser, which the visualizations read, so they see the sound as it is heard:
//
//   deck -> track gain -> fade -> equalizer (preamp, 10 bands) -> output fade -> master -> analyser
//
// Without Web Audio, the volume of the elements does gain, volume and fading of the sleep timer; the
// equalizer is unavailable and tracks change gapless instead of fading. Transcoded tracks arrive as
// HLS and are attached through hls.js. The engine knows nothing about the queue: the player store
// decides what plays.

import { crossfadeCurveSteps, equalPowerCurve } from './crossfade'
import { fallbackEqualizerBands, type EqualizerBandLayout, type EqualizerState } from './equalizer'
import { EqualizerChain } from './equalizerChain'
import { browserHlsSupport, MediaSourceSlot, type HlsSupport } from './hls'

export interface DeckSource {
    /** URL of the stream. */
    readonly url: string
    /** Gain factor of the track (ReplayGain), 1 for none. */
    readonly gain: number
    /** Whether the URL is an HLS master playlist instead of a file. */
    readonly hls?: boolean
    /** Seconds the previous track fades into this one when it ends on its own; gapless without. */
    readonly crossfade?: number
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
    /** The prepared track took over from the current one, which ended or fades out. */
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
    /** Whether the browser has Web Audio at all; assumed when not given. */
    webAudio?: boolean
}

function audioContextClass(): typeof AudioContext | undefined {
    return typeof window === 'undefined'
        ? undefined
        : (window.AudioContext ??
              (window as unknown as { webkitAudioContext?: typeof AudioContext })
                  .webkitAudioContext)
}

export const browserAudioEnvironment: AudioEnvironment = {
    createAudio: () => {
        const audio = new Audio()
        audio.preload = 'auto'
        return audio
    },
    createContext: () => {
        const Context = audioContextClass()
        try {
            return Context ? new Context({ latencyHint: 'playback' }) : null
        } catch {
            return null
        }
    },
    setTimeout: (callback, delay) => window.setTimeout(callback, delay),
    clearTimeout: (handle) => window.clearTimeout(handle),
    hls: browserHlsSupport,
    get webAudio() {
        return audioContextClass() !== undefined
    },
}

/**
 * The next track starts this many seconds before the current one ends, which roughly makes up for
 * the time a starting element needs until it sounds.
 */
export const handoverLead = 0.05

/** Seconds before the end of a track (or of its crossfade) from which the start of the next one is timed. */
export const handoverWindow = 0.6

/** Shorter crossfades than this are played as a gapless change. */
export const minimumCrossfade = 0.1

/** The fading deck is unloaded this long after its crossfade. */
export const crossfadeUnloadDelay = 0.25

/** Steps per second of the fade of the sleep timer without Web Audio. */
export const elementFadeStepsPerSecond = 10

/** Time constant in seconds with which the output returns after the sleep timer. */
export const outputRestoreSmoothing = 0.05

interface Deck {
    readonly audio: HTMLAudioElement
    gain: GainNode | null
    /** Gain of the crossfade, 1 outside of one. */
    fade: GainNode | null
    source: DeckSource | null
    /** The file or HLS stream of the element. */
    readonly slot: MediaSourceSlot
}

interface Crossfade {
    /** The deck that fades out. */
    readonly deck: Deck
    readonly timer: number
}

/** A time of a media element in seconds, 0 while unknown. */
export function finiteOrZero(value: number): number {
    return Number.isFinite(value) && value > 0 ? value : 0
}

export class AudioEngine {
    private readonly decks: [Deck, Deck]
    private current = 0
    private prepared: DeckSource | null = null
    private context: AudioContext | null = null
    private master: GainNode | null = null
    private outputFade: GainNode | null = null
    private equalizer: EqualizerChain | null = null
    private analyserNode: AnalyserNode | null = null
    private graphReady = false
    private graphFailed = false
    /** Whether the last request was play; a pause while the context resumes cancels it. */
    private wantsPlay = false
    private volume = 1
    private handover: number | null = null
    private crossfade: Crossfade | null = null
    /** A next track prepared while a crossfade still uses the other deck; set once it ended. */
    private deferredPrepare: DeckSource | null = null
    private equalizerState: EqualizerState = { enabled: false, preampDb: 0, gainsDb: [] }
    private equalizerLayout: readonly EqualizerBandLayout[] = fallbackEqualizerBands
    /** Factor of the output without Web Audio while the sleep timer fades it out. */
    private elementFade = 1
    private elementFadeTimer: number | null = null

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

    /** Whether the equalizer (and the crossfade) can work: Web Audio exists and its graph was built. */
    get equalizerAvailable(): boolean {
        return this.environment.webAudio !== false && !this.graphFailed
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

    /** Whether a next track is loaded (or waits for the end of a crossfade to be loaded). */
    get hasPrepared(): boolean {
        return this.prepared !== null || this.deferredPrepare !== null
    }

    /** Whether the previous track still fades out. */
    get crossfading(): boolean {
        return this.crossfade !== null
    }

    /** Loads a track on the current deck, optionally at a position, and starts it with play. */
    load(source: DeckSource, position = 0): void {
        this.cancelHandover()
        this.finishCrossfade()
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
        if (this.crossfade) {
            // The other deck still fades out; the track is loaded there once it is done.
            this.deferredPrepare = source
            return
        }

        const other = this.other
        if (source && sameSource(source, this.prepared)) {
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
        this.finishCrossfade()
        this.deck.audio.pause()
    }

    /** Stops and unloads both decks. */
    stop(): void {
        this.wantsPlay = false
        this.cancelHandover()
        this.finishCrossfade()
        this.prepare(null)
        this.unload(this.deck)
    }

    seek(position: number): void {
        this.cancelHandover()
        this.finishCrossfade()
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

    /**
     * Applies the equalizer at once, also during playback. Before the graph exists the settings are
     * kept and applied when it is built.
     */
    setEqualizer(state: EqualizerState, layout?: readonly EqualizerBandLayout[]): void {
        this.equalizerState = state
        if (layout) {
            this.equalizerLayout = layout
        }

        this.equalizer?.apply(state, layout)
    }

    /** Fades the output out to silence within the seconds from now on (sleep timer). */
    fadeOut(seconds: number): void {
        const duration = Math.max(0, seconds)
        if (this.outputFade && this.context) {
            const gain = this.outputFade.gain
            const now = this.context.currentTime
            gain.cancelScheduledValues(now)
            gain.setValueAtTime(gain.value, now)
            gain.linearRampToValueAtTime(0, now + duration)
            return
        }

        this.cancelElementFade()
        const steps = Math.max(1, Math.round(duration * elementFadeStepsPerSecond))
        const start = this.elementFade
        let step = 0
        const next = (): void => {
            step++
            this.elementFade = Math.max(0, start * (1 - step / steps))
            this.decks.forEach((deck) => this.applyGain(deck))
            this.elementFadeTimer =
                step < steps
                    ? this.environment.setTimeout(next, 1000 / elementFadeStepsPerSecond)
                    : null
        }
        this.elementFadeTimer = this.environment.setTimeout(next, 1000 / elementFadeStepsPerSecond)
    }

    /** Ends a fade of the sleep timer and brings the output back smoothly. */
    restoreOutput(): void {
        if (this.outputFade && this.context) {
            const gain = this.outputFade.gain
            const now = this.context.currentTime
            gain.cancelScheduledValues(now)
            gain.setValueAtTime(gain.value, now)
            gain.setTargetAtTime(1, now, outputRestoreSmoothing)
        }

        this.cancelElementFade()
        this.elementFade = 1
        this.decks.forEach((deck) => this.applyGain(deck))
    }

    private get deck(): Deck {
        return this.decks[this.current]!
    }

    private get other(): Deck {
        return this.decks[1 - this.current]!
    }

    private createDeck(index: number): Deck {
        const audio = this.environment.createAudio()
        const deck: Deck = {
            audio,
            gain: null,
            fade: null,
            source: null,
            slot: new MediaSourceSlot(audio, this.environment.hls ?? browserHlsSupport),
        }
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
        audio.addEventListener('ended', () => {
            if (active()) {
                this.onEnded()
            } else if (this.crossfade?.deck === deck) {
                // The fading track ended before its timer: the crossfade is over.
                this.finishCrossfade()
            }
        })
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
            this.graphFailed = true
            return
        }

        try {
            const master = context.createGain()
            const outputFade = context.createGain()
            const analyser = context.createAnalyser()
            const equalizer = new EqualizerChain(context, this.equalizerLayout)
            equalizer.output.connect(outputFade)
            outputFade.connect(master)
            master.connect(analyser)
            analyser.connect(context.destination)
            for (const deck of this.decks) {
                const gain = context.createGain()
                const fade = context.createGain()
                context.createMediaElementSource(deck.audio).connect(gain)
                gain.connect(fade)
                fade.connect(equalizer.input)
                deck.gain = gain
                deck.fade = fade
            }

            master.gain.value = this.volume
            outputFade.gain.value = this.elementFade
            this.cancelElementFade()
            this.elementFade = 1
            this.context = context
            this.master = master
            this.outputFade = outputFade
            this.equalizer = equalizer
            this.analyserNode = analyser
            equalizer.apply(this.equalizerState, this.equalizerLayout)
        } catch {
            // Without the graph the elements play on their own.
            this.graphFailed = true
            this.decks.forEach((deck) => {
                deck.gain = null
                deck.fade = null
            })
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
            deck.audio.volume = Math.min(1, Math.max(0, this.volume * trackGain * this.elementFade))
        }
    }

    private cancelElementFade(): void {
        if (this.elementFadeTimer !== null) {
            this.environment.clearTimeout(this.elementFadeTimer)
            this.elementFadeTimer = null
        }
    }

    /** Seconds of the crossfade into the prepared track; 0 without Web Audio. */
    private preparedCrossfade(): number {
        const seconds = this.prepared?.crossfade ?? 0
        return this.context && this.deck.fade && seconds >= minimumCrossfade ? seconds : 0
    }

    /** Starts the prepared deck shortly before the current one ends, or before its crossfade. */
    private scheduleHandover(): void {
        if (!this.prepared || this.handover !== null || this.crossfade || this.deck.audio.paused) {
            return
        }

        const audio = this.deck.audio
        const crossfade = this.preparedCrossfade()
        const remaining = finiteOrZero(audio.duration) - audio.currentTime
        if (finiteOrZero(audio.duration) === 0 || remaining > crossfade + handoverWindow) {
            return
        }

        const delay = Math.max(0, (remaining - crossfade - handoverLead) * 1000)
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
     * The prepared deck becomes the current one and plays. With a crossfade, the old one fades out
     * while the new one fades in, and is unloaded afterwards. Otherwise the old one is unloaded at
     * once if it ended, or once its last moments have played, unless it was prepared again meanwhile.
     */
    private takeOver(ended: boolean): void {
        if (!this.prepared) {
            return
        }

        const previous = this.deck
        const remaining = finiteOrZero(previous.audio.duration) - previous.audio.currentTime
        const crossfade = ended ? 0 : Math.min(this.preparedCrossfade(), Math.max(0, remaining))
        this.current = 1 - this.current
        this.prepared = null
        const audio = this.deck.audio
        if (crossfade >= minimumCrossfade) {
            this.startCrossfade(previous, crossfade)
        } else {
            this.setFade(this.deck, 1)
        }

        void audio.play().catch((error: unknown) => this.events.error?.(error))
        previous.source = null
        if (ended) {
            this.unload(previous)
        } else if (crossfade < minimumCrossfade) {
            this.environment.setTimeout(() => {
                if (
                    previous !== this.deck &&
                    previous.source === null &&
                    this.crossfade?.deck !== previous
                ) {
                    this.unload(previous)
                }
            }, 1000)
        }

        this.events.advanced?.()
        this.events.timeupdate?.(finiteOrZero(audio.currentTime), finiteOrZero(audio.duration))
    }

    private startCrossfade(previous: Deck, seconds: number): void {
        const now = this.context?.currentTime ?? 0
        this.rampFade(this.deck, equalPowerCurve('in', crossfadeCurveSteps), now, seconds)
        this.rampFade(previous, equalPowerCurve('out', crossfadeCurveSteps), now, seconds)
        const timer = this.environment.setTimeout(
            () => this.finishCrossfade(),
            (seconds + crossfadeUnloadDelay) * 1000,
        )
        this.crossfade = { deck: previous, timer }
    }

    /**
     * Ends a crossfade at once: the old track stops and is unloaded, the current one plays at full
     * level. A track prepared meanwhile is loaded on the freed deck.
     */
    private finishCrossfade(): void {
        const crossfade = this.crossfade
        if (!crossfade) {
            return
        }

        this.crossfade = null
        this.environment.clearTimeout(crossfade.timer)
        if (crossfade.deck !== this.deck) {
            crossfade.deck.audio.pause()
            this.unload(crossfade.deck)
            this.setFade(crossfade.deck, 1)
        }

        this.setFade(this.deck, 1)
        const deferred = this.deferredPrepare
        this.deferredPrepare = null
        if (deferred) {
            this.prepare(deferred)
        }
    }

    private rampFade(deck: Deck, curve: Float32Array, start: number, seconds: number): void {
        const fade = deck.fade?.gain
        if (!fade) {
            return
        }

        fade.cancelScheduledValues(start)
        fade.setValueCurveAtTime(curve, start, seconds)
    }

    private setFade(deck: Deck, value: number): void {
        const fade = deck.fade?.gain
        if (!fade) {
            return
        }

        const now = this.context?.currentTime ?? 0
        fade.cancelScheduledValues(now)
        fade.setValueAtTime(value, now)
    }

    /** Sets the source of a deck: a file directly, an HLS stream through the HLS support. */
    private attach(deck: Deck, source: DeckSource): void {
        deck.slot.set(source.url, source.hls === true, (error) => {
            if (this.decks[this.current] === deck) {
                this.events.error?.(error)
            }
        })
    }

    private unload(deck: Deck): void {
        deck.source = null
        deck.slot.clear()
    }
}

function sameSource(source: DeckSource, other: DeckSource | null): boolean {
    return (
        !!other &&
        source.url === other.url &&
        source.gain === other.gain &&
        (source.crossfade ?? 0) === (other.crossfade ?? 0)
    )
}
