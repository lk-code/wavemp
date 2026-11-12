import Sortable from 'sortablejs'
import { onBeforeUnmount, watch, type Ref } from 'vue'

export interface SortableListOptions {
    /** Selector of the elements that can be dragged, e.g. the rows of a list. */
    readonly draggable: string
    /** Selector of the handle within an element; only the handle starts dragging. */
    readonly handle: string
    /** Called after an element was dropped at another place, with its old and new index. */
    readonly onMove: (from: number, to: number) => void
    /** While true, nothing can be dragged. */
    readonly disabled?: Ref<boolean>
}

/**
 * Reorders the elements of a container by drag and drop, with mouse, pen and touch (SortableJS).
 * Vue owns the DOM: after a drop the element goes back to its old place and onMove reports the
 * move, so the component changes its data and Vue renders the new order.
 */
export function useSortableList(
    container: Ref<HTMLElement | null | undefined>,
    options: SortableListOptions,
): void {
    let sortable: Sortable | null = null

    function destroy(): void {
        sortable?.destroy()
        sortable = null
    }

    watch(
        container,
        (element) => {
            destroy()
            if (!element) {
                return
            }

            sortable = Sortable.create(element, {
                draggable: options.draggable,
                handle: options.handle,
                animation: 150,
                disabled: options.disabled?.value ?? false,
                onEnd(event) {
                    const from = event.oldDraggableIndex
                    const to = event.newDraggableIndex
                    if (from === undefined || to === undefined || from === to) {
                        return
                    }

                    const { item, from: list } = event
                    const elements = [...list.children].filter((child) => child !== item)
                    const anchor = elements.filter((child) => child.matches(options.draggable))[
                        from
                    ]
                    list.insertBefore(item, anchor ?? null)
                    options.onMove(from, to)
                },
            })
        },
        { immediate: true, flush: 'post' },
    )

    if (options.disabled) {
        watch(options.disabled, (disabled) => sortable?.option('disabled', disabled))
    }

    onBeforeUnmount(destroy)
}
