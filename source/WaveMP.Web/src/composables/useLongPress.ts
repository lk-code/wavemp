import { getCurrentInstance, onBeforeUnmount } from 'vue'

/** Time a finger has to rest on an element for a long press, in milliseconds. */
export const longPressDelay = 500

/** Distance in pixels a finger may move without cancelling the long press. */
export const longPressTolerance = 10

/**
 * Long press with a finger (touch pointers only), e.g. to open a context menu where there is no
 * right click. Moving the finger, lifting it or a cancelled pointer ends the press without effect.
 * The payload (e.g. the id of a row) is handed to the callback with the point of the press.
 */
export function useLongPress<T>(
    onLongPress: (payload: T, x: number, y: number) => void,
    delay = longPressDelay,
) {
    let timer: ReturnType<typeof setTimeout> | undefined
    let start: { x: number; y: number } | null = null

    function cancel(): void {
        clearTimeout(timer)
        timer = undefined
        start = null
    }

    function onPointerdown(event: PointerEvent, payload: T): void {
        if (event.pointerType !== 'touch') {
            return
        }

        cancel()
        const point = { x: event.clientX, y: event.clientY }
        start = point
        timer = setTimeout(() => {
            timer = undefined
            start = null
            onLongPress(payload, point.x, point.y)
        }, delay)
    }

    function onPointermove(event: PointerEvent): void {
        if (
            start &&
            Math.hypot(event.clientX - start.x, event.clientY - start.y) > longPressTolerance
        ) {
            cancel()
        }
    }

    if (getCurrentInstance()) {
        onBeforeUnmount(cancel)
    }

    return { onPointerdown, onPointermove, cancel }
}
