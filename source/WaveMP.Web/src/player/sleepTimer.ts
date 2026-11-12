// The sleep timer of the player: stops the playback after a time, at the end of the current track or
// at the end of the queue, and fades the music out before. It belongs to the device that plays and is
// not stored. Framework-free; the player store connects it to the engine and the queue.

export type SleepTimerMode = 'off' | 'duration' | 'endOfTrack' | 'endOfQueue'

/** Minutes the menu offers. */
export const sleepTimerDurations: readonly number[] = [15, 30, 45, 60, 90, 120]

/** Seconds of the fade-out as long as the settings are unknown. */
export const defaultSleepTimerFadeSeconds = 10

export interface SleepTimerSettings {
    /** Seconds the music fades out before it stops. */
    readonly fadeSeconds: number
}

export const defaultSleepTimerSettings: SleepTimerSettings = {
    fadeSeconds: defaultSleepTimerFadeSeconds,
}

/** The sleep timer settings from an answer of the server, or null if they are incomplete. */
export function toSleepTimerSettings(value: unknown): SleepTimerSettings | null {
    const fadeSeconds = (value as { fadeSeconds?: unknown } | null | undefined)?.fadeSeconds
    return typeof fadeSeconds === 'number' &&
        Number.isInteger(fadeSeconds) &&
        fadeSeconds >= 1 &&
        fadeSeconds <= 60
        ? { fadeSeconds }
        : null
}

export interface SleepTimerState {
    readonly mode: SleepTimerMode
    /** When the timer of the mode duration ends, in milliseconds since the epoch. */
    readonly endsAt: number | null
    /** Whether the music fades out right now. */
    readonly fading: boolean
}

export const sleepTimerOff: SleepTimerState = { mode: 'off', endsAt: null, fading: false }

export interface SleepTimerHooks {
    /** Fades the music out within the seconds. */
    fadeOut(seconds: number): void
    /** Brings the music back to its level, e.g. after the stop or when cancelled. */
    restore(): void
    /** Pauses the playback at its position. */
    pause(): void
    /** The state changed. */
    changed?(state: SleepTimerState): void
}

export interface SleepTimerClock {
    now(): number
    setTimeout(callback: () => void, delay: number): number
    clearTimeout(handle: number): void
}

export const browserSleepTimerClock: SleepTimerClock = {
    now: () => Date.now(),
    setTimeout: (callback, delay) => window.setTimeout(callback, delay),
    clearTimeout: (handle) => window.clearTimeout(handle),
}

/**
 * The timer. With a duration it fades out and pauses on its own; timers of a hidden page may come
 * late, so the player also calls check with every time update. For the ends of a track or the queue,
 * the player reports the progress of the current track and stops at its end.
 */
export class SleepTimer {
    private current: SleepTimerState = sleepTimerOff
    private fadeTimer: number | null = null
    private endTimer: number | null = null

    constructor(
        private readonly hooks: SleepTimerHooks,
        private readonly fadeSeconds: () => number = () => defaultSleepTimerFadeSeconds,
        private readonly clock: SleepTimerClock = browserSleepTimerClock,
    ) {}

    get state(): SleepTimerState {
        return this.current
    }

    get active(): boolean {
        return this.current.mode !== 'off'
    }

    /** Starts the timer; another running timer is replaced. A duration takes minutes. */
    start(mode: Exclude<SleepTimerMode, 'off'>, minutes = 0): void {
        const wasFading = this.current.fading
        this.clearTimers()
        if (wasFading) {
            this.hooks.restore()
        }

        if (mode !== 'duration') {
            this.set({ mode, endsAt: null, fading: false })
            return
        }

        const endsAt = this.clock.now() + Math.max(0, minutes) * 60_000
        this.set({ mode, endsAt, fading: false })
        const fadeAt = Math.max(0, endsAt - this.fadeMilliseconds() - this.clock.now())
        this.fadeTimer = this.clock.setTimeout(() => {
            this.fadeTimer = null
            this.check()
        }, fadeAt)
        this.endTimer = this.clock.setTimeout(
            () => {
                this.endTimer = null
                this.check()
            },
            Math.max(0, endsAt - this.clock.now()),
        )
    }

    /** Stops the timer; a running fade-out returns smoothly. */
    cancel(): void {
        if (!this.active) {
            return
        }

        const wasFading = this.current.fading
        this.clearTimers()
        this.set(sleepTimerOff)
        if (wasFading) {
            this.hooks.restore()
        }
    }

    /** Checks a timer with a duration: fades out shortly before its end and pauses at its end. */
    check(): void {
        const { mode, endsAt, fading } = this.current
        if (mode !== 'duration' || endsAt === null) {
            return
        }

        const now = this.clock.now()
        if (now >= endsAt) {
            this.clearTimers()
            this.set(sleepTimerOff)
            this.hooks.pause()
            this.hooks.restore()
            return
        }

        if (!fading && now >= endsAt - this.fadeMilliseconds()) {
            this.set({ ...this.current, fading: true })
            this.hooks.fadeOut((endsAt - now) / 1000)
        }
    }

    /**
     * Whether the playback stops after the current track: at the end of every track, or at the end of
     * the last entry of the queue (repeat does not count).
     */
    stopsAfterCurrent(isLastEntry: boolean): boolean {
        return (
            this.current.mode === 'endOfTrack' ||
            (this.current.mode === 'endOfQueue' && isLastEntry)
        )
    }

    /** Progress of the current track: fades out its last seconds if the playback stops after it. */
    progress(remaining: number, isLastEntry: boolean): void {
        if (this.current.mode === 'off' || this.current.mode === 'duration') {
            return
        }

        const fade = this.fadeSeconds()
        const stops = this.stopsAfterCurrent(isLastEntry)
        if (this.current.fading) {
            // Seeked back or the queue grew: the music comes back until the next fade.
            if (!stops || remaining > fade + 1) {
                this.set({ ...this.current, fading: false })
                this.hooks.restore()
            }

            return
        }

        if (!stops || !Number.isFinite(remaining) || remaining <= 0) {
            return
        }

        if (remaining <= fade) {
            this.set({ ...this.current, fading: true })
            this.hooks.fadeOut(remaining)
        }
    }

    /** The track the timer waited for ended and the player stopped: the timer is done. */
    trackEnded(): void {
        if (this.current.mode === 'endOfTrack' || this.current.mode === 'endOfQueue') {
            this.clearTimers()
            this.set(sleepTimerOff)
            this.hooks.restore()
        }
    }

    private fadeMilliseconds(): number {
        return Math.max(0, this.fadeSeconds()) * 1000
    }

    private set(state: SleepTimerState): void {
        this.current = state
        this.hooks.changed?.(state)
    }

    private clearTimers(): void {
        if (this.fadeTimer !== null) {
            this.clock.clearTimeout(this.fadeTimer)
            this.fadeTimer = null
        }

        if (this.endTimer !== null) {
            this.clock.clearTimeout(this.endTimer)
            this.endTimer = null
        }
    }
}
