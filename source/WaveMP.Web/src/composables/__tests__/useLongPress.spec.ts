import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { longPressDelay, useLongPress } from '../useLongPress'

function pointer(type: string, x: number, y: number, pointerType = 'touch'): PointerEvent {
    return { type, clientX: x, clientY: y, pointerType } as PointerEvent
}

describe('useLongPress', () => {
    beforeEach(() => {
        vi.useFakeTimers()
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    it('reports a finger that rests on the element with its payload and point', () => {
        const callback = vi.fn()
        const press = useLongPress<string>(callback)

        press.onPointerdown(pointer('pointerdown', 10, 20), 'row-1')
        vi.advanceTimersByTime(longPressDelay)

        expect(callback).toHaveBeenCalledWith('row-1', 10, 20)
    })

    it('ignores mouse pointers, released fingers and moved fingers', () => {
        const callback = vi.fn()
        const press = useLongPress<string>(callback)

        press.onPointerdown(pointer('pointerdown', 0, 0, 'mouse'), 'mouse')
        vi.advanceTimersByTime(longPressDelay)

        press.onPointerdown(pointer('pointerdown', 0, 0), 'released')
        vi.advanceTimersByTime(longPressDelay - 100)
        press.cancel()
        vi.advanceTimersByTime(200)

        press.onPointerdown(pointer('pointerdown', 0, 0), 'moved')
        press.onPointermove(pointer('pointermove', 4, 3))
        press.onPointermove(pointer('pointermove', 30, 0))
        vi.advanceTimersByTime(longPressDelay)

        expect(callback).not.toHaveBeenCalled()
    })
})
