import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { usePolling } from '../usePolling'

function setHidden(hidden: boolean): void {
    Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden })
    document.dispatchEvent(new Event('visibilitychange'))
}

describe('usePolling', () => {
    beforeEach(() => {
        vi.useFakeTimers()
        setHidden(false)
    })

    afterEach(() => {
        vi.useRealTimers()
        setHidden(false)
    })

    it('calls the callback every interval while started', async () => {
        const callback = vi.fn()
        const polling = usePolling(callback, 1000)

        polling.start()
        await vi.advanceTimersByTimeAsync(3500)
        polling.stop()
        await vi.advanceTimersByTimeAsync(3000)

        expect(callback).toHaveBeenCalledTimes(3)
        expect(polling.active.value).toBe(false)
    })

    it('pauses while the page is hidden and calls at once when it is visible again', async () => {
        const callback = vi.fn()
        const polling = usePolling(callback, 1000)
        polling.start()

        setHidden(true)
        await vi.advanceTimersByTimeAsync(5000)
        const whileHidden = callback.mock.calls.length
        setHidden(false)
        await vi.advanceTimersByTimeAsync(0)

        expect(whileHidden).toBe(0)
        expect(callback).toHaveBeenCalledOnce()
        polling.stop()
    })

    it('keeps polling after a failing call', async () => {
        const callback = vi.fn().mockRejectedValueOnce(new Error('offline'))
        const polling = usePolling(callback, 1000)

        polling.start()
        await vi.advanceTimersByTimeAsync(2500)

        expect(callback).toHaveBeenCalledTimes(2)
        polling.stop()
    })

    it('stops with its scope', async () => {
        const callback = vi.fn()
        const scope = effectScope()
        const polling = scope.run(() => usePolling(callback, 1000))!

        polling.start()
        scope.stop()
        await vi.advanceTimersByTimeAsync(3000)

        expect(callback).not.toHaveBeenCalled()
        expect(polling.active.value).toBe(false)
    })
})
