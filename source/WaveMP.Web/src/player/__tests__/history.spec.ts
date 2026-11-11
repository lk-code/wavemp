import { describe, expect, it, vi } from 'vitest'
import { PlayHistoryTracker } from '../history'

const startedAt = new Date('2026-11-11T20:00:00Z')

/** Plays from 0 to the end position in steps of a quarter second. */
function listen(tracker: PlayHistoryTracker, to: number, duration: number, from = 0): void {
    for (let position = from; position <= to; position += 0.25) {
        tracker.progress(position, duration)
    }
}

describe('PlayHistoryTracker', () => {
    it('reports a play once after half the track with the time it started', async () => {
        const report = vi.fn().mockResolvedValue(undefined)
        const tracker = new PlayHistoryTracker(report, () => startedAt)

        tracker.start('t1')
        listen(tracker, 29, 60)
        expect(report).not.toHaveBeenCalled()
        listen(tracker, 59, 60, 29.25)
        await tracker.flush()

        expect(report).toHaveBeenCalledOnce()
        expect(report).toHaveBeenCalledWith('t1', startedAt)
    })

    it('does not count seeking', () => {
        const report = vi.fn().mockResolvedValue(undefined)
        const tracker = new PlayHistoryTracker(report, () => startedAt)

        tracker.start('t1')
        listen(tracker, 5, 60)
        tracker.seek(55)
        listen(tracker, 60, 60, 55)

        expect(report).not.toHaveBeenCalled()
    })

    it('counts a new play of the same track again', async () => {
        const report = vi.fn().mockResolvedValue(undefined)
        const tracker = new PlayHistoryTracker(report, () => startedAt)

        tracker.start('t1')
        listen(tracker, 2, 2)
        tracker.start('t1')
        listen(tracker, 2, 2)
        await tracker.flush()

        expect(report).toHaveBeenCalledTimes(2)
    })

    it('keeps failed reports and sends them later', async () => {
        const report = vi
            .fn()
            .mockRejectedValueOnce(new TypeError('offline'))
            .mockResolvedValue(undefined)
        const tracker = new PlayHistoryTracker(report, () => startedAt)

        tracker.start('t1')
        listen(tracker, 2, 2)
        await tracker.flush()
        expect(tracker.pendingCount).toBe(1)

        await tracker.flush()
        expect(tracker.pendingCount).toBe(0)
        expect(report).toHaveBeenCalledTimes(2)
    })

    it('drops reports the server rejects for good', async () => {
        const report = vi.fn().mockRejectedValue({ responseStatusCode: 404 })
        const tracker = new PlayHistoryTracker(report, () => startedAt)

        tracker.start('t1')
        listen(tracker, 2, 2)
        await tracker.flush()

        expect(tracker.pendingCount).toBe(0)
    })

    it('reports nothing after stop', () => {
        const report = vi.fn().mockResolvedValue(undefined)
        const tracker = new PlayHistoryTracker(report, () => startedAt)

        tracker.start('t1')
        tracker.stop()
        listen(tracker, 2, 2)

        expect(report).not.toHaveBeenCalled()
    })
})
