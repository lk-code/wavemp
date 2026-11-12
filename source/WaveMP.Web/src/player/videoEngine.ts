// The video engine of the player: the video element of the Now Playing stage. It offers the same
// operations as the audio engine, but with one element only: a video is not prepared ahead, the
// next track loads when the video ended. The video file plays as it is, a transcoded video as HLS.
// The volume of the element applies volume and track gain.

import { finiteOrZero, type DeckSource, type EngineEvents } from './engine'
import { browserHlsSupport, MediaSourceSlot, type HlsSupport } from './hls'

export class VideoEngine {
    private readonly slot: MediaSourceSlot
    private readonly listeners = new AbortController()
    private current: DeckSource | null = null
    private volume = 1

    constructor(
        readonly element: HTMLVideoElement,
        private readonly events: EngineEvents = {},
        hls: HlsSupport = browserHlsSupport,
    ) {
        this.slot = new MediaSourceSlot(element, hls)
        const signal = this.listeners.signal
        const time = (): void => {
            if (this.current) {
                this.events.timeupdate?.(this.position, finiteOrZero(element.duration))
            }
        }
        element.addEventListener('timeupdate', time, { signal })
        element.addEventListener('durationchange', time, { signal })
        element.addEventListener('playing', () => this.current && this.events.playing?.(), {
            signal,
        })
        element.addEventListener(
            'pause',
            () => {
                if (this.current && !element.ended) {
                    this.events.paused?.()
                }
            },
            { signal },
        )
        element.addEventListener('waiting', () => this.current && this.events.waiting?.(), {
            signal,
        })
        element.addEventListener('ended', () => this.current && this.events.ended?.(), { signal })
        element.addEventListener(
            'error',
            () => {
                if (this.current) {
                    this.events.error?.(element.error ?? new Error('The video failed to play.'))
                }
            },
            { signal },
        )
    }

    /** The loaded source. */
    get source(): DeckSource | null {
        return this.current
    }

    get position(): number {
        return finiteOrZero(this.element.currentTime)
    }

    /** Videos are never prepared ahead. */
    get hasPrepared(): boolean {
        return false
    }

    /** Loads a video, optionally at a position, and starts it with play. */
    load(source: DeckSource, position = 0): void {
        this.current = source
        this.applyVolume()
        this.slot.set(source.url, source.hls === true, (error) => {
            if (this.current) {
                this.events.error?.(error)
            }
        })
        if (position > 0) {
            const element = this.element
            element.addEventListener(
                'loadedmetadata',
                () => {
                    element.currentTime = position
                },
                { once: true, signal: this.listeners.signal },
            )
        }
    }

    /** Takes no next track: it loads when the video ended. */
    prepare(): void {
        // Videos are not prepared ahead.
    }

    async play(): Promise<void> {
        try {
            await this.element.play()
        } catch (error) {
            // An interrupted play (pause or new source meanwhile) is no error.
            if (!(error instanceof DOMException && error.name === 'AbortError')) {
                this.events.error?.(error)
            }
        }
    }

    pause(): void {
        this.element.pause()
    }

    /** Stops and unloads the video. */
    stop(): void {
        this.current = null
        this.slot.clear()
    }

    seek(position: number): void {
        const duration = finiteOrZero(this.element.duration)
        this.element.currentTime = Math.max(
            0,
            duration > 0 ? Math.min(position, duration) : position,
        )
    }

    /** Output gain between 0 and 1 (already shaped by the volume curve). */
    setVolume(gain: number): void {
        this.volume = gain
        this.applyVolume()
    }

    setTrackGain(gain: number): void {
        if (this.current) {
            this.current = { ...this.current, gain }
            this.applyVolume()
        }
    }

    /** Stops the video and lets go of the element, e.g. when the stage closes. */
    dispose(): void {
        this.stop()
        this.listeners.abort()
    }

    private applyVolume(): void {
        const gain = this.current?.gain ?? 1
        this.element.volume = Math.min(1, Math.max(0, this.volume * gain))
    }
}
