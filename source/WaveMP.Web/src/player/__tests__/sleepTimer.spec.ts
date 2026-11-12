import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
    SleepTimer,
    toSleepTimerSettings,
    type SleepTimerClock,
    type SleepTimerState,
} from '../sleepTimer'

/** A clock the tests move forward; due timers run in order. */
class FakeClock implements SleepTimerClock {
    time = 1_000_000
    private next = 1
    private readonly timers = new Map<number, { at: number; callback: () => void }>()

    now(): number {
        return this.time
    }

    setTimeout(callback: () => void, delay: number): number {
        const handle = this.next++
        this.timers.set(handle, { at: this.time + delay, callback })
        return handle
    }

    clearTimeout(handle: number): void {
        this.timers.delete(handle)
    }

    /** Moves the time; with timers false, they do not fire (e.g. throttled in a hidden page). */
    advance(milliseconds: number, timers = true): void {
        this.time += milliseconds
        if (!timers) {
            return
        }

        for (const [handle, timer] of [...this.timers].sort((a, b) => a[1].at - b[1].at)) {
            if (timer.at <= this.time && this.timers.has(handle)) {
                this.timers.delete(handle)
                timer.callback()
            }
        }
    }

    get pending(): number {
        return this.timers.size
    }
}

describe('SleepTimer', () => {
    let clock: FakeClock
    let hooks: {
        fadeOut: ReturnType<typeof vi.fn<(seconds: number) => void>>
        restore: ReturnType<typeof vi.fn<() => void>>
        pause: ReturnType<typeof vi.fn<() => void>>
        changed: ReturnType<typeof vi.fn<(state: SleepTimerState) => void>>
    }
    let states: SleepTimerState[]
    let timer: SleepTimer
    let fade = 10

    beforeEach(() => {
        clock = new FakeClock()
        states = []
        fade = 10
        hooks = {
            fadeOut: vi.fn(),
            restore: vi.fn(),
            pause: vi.fn(),
            changed: vi.fn((state: SleepTimerState) => {
                states.push(state)
            }),
        }
        timer = new SleepTimer(hooks, () => fade, clock)
    })

    describe('with a duration', () => {
        it('fades out before the end and pauses at the end', () => {
            timer.start('duration', 15)

            expect(timer.state).toEqual({
                mode: 'duration',
                endsAt: clock.time + 15 * 60_000,
                fading: false,
            })
            clock.advance(15 * 60_000 - 10_001)
            expect(hooks.fadeOut).not.toHaveBeenCalled()

            clock.advance(1)
            expect(hooks.fadeOut).toHaveBeenCalledWith(10)
            expect(timer.state.fading).toBe(true)

            clock.advance(10_000)
            expect(hooks.pause).toHaveBeenCalledOnce()
            expect(hooks.restore).toHaveBeenCalledOnce()
            expect(hooks.pause.mock.invocationCallOrder[0]).toBeLessThan(
                hooks.restore.mock.invocationCallOrder[0]!,
            )
            expect(timer.state.mode).toBe('off')
            expect(clock.pending).toBe(0)
        })

        it('catches up with late timers through check', () => {
            timer.start('duration', 30)

            clock.advance(30 * 60_000 - 4_000, false)
            timer.check()
            expect(hooks.fadeOut).toHaveBeenCalledWith(4)

            clock.advance(5_000, false)
            timer.check()
            expect(hooks.pause).toHaveBeenCalledOnce()
            expect(timer.active).toBe(false)
        })

        it('brings the music back when cancelled during the fade', () => {
            timer.start('duration', 15)
            clock.advance(15 * 60_000 - 5_000)

            timer.cancel()

            expect(hooks.restore).toHaveBeenCalledOnce()
            expect(hooks.pause).not.toHaveBeenCalled()
            expect(timer.state).toEqual({ mode: 'off', endsAt: null, fading: false })
            expect(clock.pending).toBe(0)
        })

        it('replaces a running timer', () => {
            timer.start('duration', 15)
            timer.start('duration', 60)

            clock.advance(16 * 60_000)

            expect(hooks.pause).not.toHaveBeenCalled()
            expect(timer.state.mode).toBe('duration')
        })
    })

    describe('at the end of the track', () => {
        it('stops after every track and fades its last seconds', () => {
            timer.start('endOfTrack')

            expect(timer.stopsAfterCurrent(false)).toBe(true)
            timer.progress(30, false)
            expect(hooks.fadeOut).not.toHaveBeenCalled()
            timer.progress(8, false)
            expect(hooks.fadeOut).toHaveBeenCalledWith(8)
            expect(timer.state.fading).toBe(true)

            timer.trackEnded()
            expect(hooks.restore).toHaveBeenCalledOnce()
            expect(timer.state.mode).toBe('off')
        })

        it('brings the music back after seeking back during the fade', () => {
            timer.start('endOfTrack')
            timer.progress(5, false)

            timer.progress(60, false)

            expect(hooks.restore).toHaveBeenCalledOnce()
            expect(timer.state.fading).toBe(false)
            expect(timer.state.mode).toBe('endOfTrack')
        })
    })

    describe('at the end of the queue', () => {
        it('stops only after the last entry', () => {
            timer.start('endOfQueue')

            expect(timer.stopsAfterCurrent(false)).toBe(false)
            timer.progress(3, false)
            expect(hooks.fadeOut).not.toHaveBeenCalled()

            expect(timer.stopsAfterCurrent(true)).toBe(true)
            timer.progress(3, true)
            expect(hooks.fadeOut).toHaveBeenCalledWith(3)
        })

        it('does nothing at the end of a track with duration or off', () => {
            timer.trackEnded()
            timer.start('duration', 15)
            timer.trackEnded()

            expect(timer.state.mode).toBe('duration')
            expect(hooks.restore).not.toHaveBeenCalled()
        })
    })

    it('reports every change', () => {
        timer.start('endOfQueue')
        timer.cancel()

        expect(states.map((state) => state.mode)).toEqual(['endOfQueue', 'off'])
    })

    it('reads complete settings only', () => {
        expect(toSleepTimerSettings({ fadeSeconds: 30 })).toEqual({ fadeSeconds: 30 })
        expect(toSleepTimerSettings({ fadeSeconds: 0 })).toBeNull()
        expect(toSleepTimerSettings(null)).toBeNull()
    })
})
