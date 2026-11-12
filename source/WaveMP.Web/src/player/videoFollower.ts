// The video of the Now Playing stage. The audio engine always plays the track; the video element
// only shows the picture: muted, at the position of the audio plus the offset of the track. While
// the stage shows the cover or the visualization, the video waits paused with its source loaded, so
// switching back is quick. A video file plays as it is, a transcoded video as HLS.

import { finiteOrZero } from './engine'
import { browserHlsSupport, MediaSourceSlot, type HlsSupport } from './hls'
import { softSyncThreshold, syncAction, type VideoState } from './videoSync'

/** The audio the video follows. */
export interface FollowerClock {
    /** Position of the audio in seconds. */
    readonly position: number
    /** Whether the audio plays. */
    readonly playing: boolean
}

export interface VideoFollowerEvents {
    /** The video is loading or positioning, ready at the right position, failed or unloaded. */
    stateChange?(state: VideoState): void
    /** The video was started by its own controls (e.g. the native full screen on the iPhone). */
    userPlay?(): void
    /** The video was paused by its own controls. */
    userPause?(): void
}

/** The source of a video. */
export interface VideoFollowerSource {
    readonly url: string
    /** Whether the URL is an HLS master playlist instead of a file. */
    readonly hls?: boolean
}

/** What the follower needs from the browser; tests replace it. */
export interface FollowerEnvironment {
    setInterval(callback: () => void, delay: number): number
    clearInterval(handle: number): void
    /** Milliseconds of a monotonic clock. */
    now(): number
    hls: HlsSupport
    /** Whether the video shows its own controls, e.g. in the native full screen on the iPhone. */
    hasOwnControls(element: HTMLVideoElement): boolean
}

export const browserFollowerEnvironment: FollowerEnvironment = {
    setInterval: (callback, delay) => window.setInterval(callback, delay),
    clearInterval: (handle) => window.clearInterval(handle),
    now: () => performance.now(),
    hls: browserHlsSupport,
    hasOwnControls: (element) =>
        (element as unknown as { webkitDisplayingFullscreen?: boolean })
            .webkitDisplayingFullscreen === true,
}

/** Milliseconds between two checks of the position while the video is shown. */
export const syncInterval = 250

/** readyState from which the element shows the picture of its position (HAVE_CURRENT_DATA). */
const haveCurrentData = 2

export class VideoFollower {
    private readonly slot: MediaSourceSlot
    private readonly listeners = new AbortController()
    private state: VideoState = 'idle'
    private loaded = false
    private hasMetadata = false
    private active = false
    /** The video was hidden and is about to be shown: a seek keeps it hidden until it is done. */
    private revealing = false
    /** Whether the follower itself wants the video to play; other plays and pauses are the user's. */
    private wantsPlaying = false
    private offset = 0
    private lastSeek = Number.NEGATIVE_INFINITY
    private timer: number | null = null

    constructor(
        readonly element: HTMLVideoElement,
        private readonly clock: () => FollowerClock,
        private readonly events: VideoFollowerEvents = {},
        private readonly environment: FollowerEnvironment = browserFollowerEnvironment,
    ) {
        element.muted = true
        element.defaultMuted = true
        element.playsInline = true
        this.slot = new MediaSourceSlot(element, environment.hls)
        const signal = this.listeners.signal
        element.addEventListener(
            'loadedmetadata',
            () => {
                if (this.loaded) {
                    this.hasMetadata = true
                    this.revealing = true
                    this.sync()
                    this.checkReady()
                }
            },
            { signal },
        )
        for (const name of ['loadeddata', 'canplay', 'seeked', 'playing']) {
            element.addEventListener(name, () => this.checkReady(), { signal })
        }
        element.addEventListener(
            'play',
            () => {
                if (this.loaded && !this.wantsPlaying && environment.hasOwnControls(element)) {
                    this.events.userPlay?.()
                }
            },
            { signal },
        )
        element.addEventListener(
            'pause',
            () => {
                if (
                    this.loaded &&
                    this.wantsPlaying &&
                    !element.ended &&
                    environment.hasOwnControls(element)
                ) {
                    this.wantsPlaying = false
                    this.events.userPause?.()
                }
            },
            { signal },
        )
        element.addEventListener('error', () => this.fail(), { signal })
    }

    /** State of the video. */
    get videoState(): VideoState {
        return this.state
    }

    /** Loads a video; offset in milliseconds (position of the video = audio + offset). */
    load(source: VideoFollowerSource, offsetMilliseconds = 0): void {
        this.unload()
        this.loaded = true
        this.offset = offsetMilliseconds / 1000
        this.setState('loading')
        this.slot.set(source.url, source.hls === true, () => this.fail())
    }

    /** Stops and removes the video. */
    unload(): void {
        this.stopTimer()
        this.wantsPlaying = false
        this.loaded = false
        this.hasMetadata = false
        this.revealing = false
        this.lastSeek = Number.NEGATIVE_INFINITY
        this.slot.clear()
        this.setState('idle')
    }

    /** A new offset in milliseconds; it applies with the next check. */
    setOffset(offsetMilliseconds: number): void {
        this.offset = offsetMilliseconds / 1000
    }

    /**
     * Shows (true) or hides (false) the video. A hidden video waits paused with its source loaded; a
     * shown one is positioned at once and checked regularly.
     */
    setActive(active: boolean): void {
        if (!active) {
            this.active = false
            this.stopTimer()
            this.wantsPlaying = false
            this.element.pause()
            this.element.playbackRate = 1
            return
        }

        if (!this.active) {
            this.active = true
            this.revealing = true
        }

        this.sync()
        this.timer ??= this.environment.setInterval(() => this.sync(), syncInterval)
    }

    /** Brings the video to the position of the audio and plays or pauses it like the audio. */
    sync(): void {
        if (!this.active || !this.loaded || !this.hasMetadata || this.state === 'failed') {
            return
        }

        const element = this.element
        if (element.seeking) {
            return
        }

        const clock = this.clock()
        const action = syncAction({
            audioPosition: clock.position,
            playing: clock.playing,
            videoPosition: finiteOrZero(element.currentTime),
            offset: this.offset,
            videoDuration: finiteOrZero(element.duration),
            sinceLastSeek: (this.environment.now() - this.lastSeek) / 1000,
        })
        const revealing = this.revealing
        this.revealing = false
        if (action.kind === 'hold') {
            element.playbackRate = 1
            this.pause()
            if (Math.abs(finiteOrZero(element.currentTime) - action.at) > softSyncThreshold) {
                this.seek(action.at, revealing)
            }
        } else if (action.kind === 'seek') {
            element.playbackRate = 1
            this.seek(action.to, revealing)
            this.follow(clock.playing)
        } else {
            element.playbackRate = clock.playing ? action.rate : 1
            this.follow(clock.playing)
        }

        this.checkReady()
    }

    /** Stops the video and lets go of the element, e.g. when the stage closes. */
    dispose(): void {
        this.unload()
        this.active = false
        this.listeners.abort()
    }

    private seek(to: number, hidden: boolean): void {
        this.lastSeek = this.environment.now()
        this.element.currentTime = to
        // A video that is about to be shown stays hidden until it shows the right picture.
        if (hidden && this.state === 'ready') {
            this.setState('loading')
        }
    }

    private follow(playing: boolean): void {
        if (playing) {
            this.play()
        } else {
            this.pause()
        }
    }

    private play(): void {
        this.wantsPlaying = true
        if (this.element.paused) {
            this.element.play().catch(() => {
                // An interrupted start is no error; the next check starts the video again.
            })
        }
    }

    private pause(): void {
        this.wantsPlaying = false
        if (!this.element.paused) {
            this.element.pause()
        }
    }

    private checkReady(): void {
        const element = this.element
        if (
            this.state === 'loading' &&
            this.hasMetadata &&
            !element.seeking &&
            element.readyState >= haveCurrentData
        ) {
            this.setState('ready')
        }
    }

    private fail(): void {
        if (this.loaded && this.state !== 'failed') {
            this.stopTimer()
            this.wantsPlaying = false
            this.setState('failed')
        }
    }

    private stopTimer(): void {
        if (this.timer !== null) {
            this.environment.clearInterval(this.timer)
            this.timer = null
        }
    }

    private setState(state: VideoState): void {
        if (this.state !== state) {
            this.state = state
            this.events.stateChange?.(state)
        }
    }
}
