import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, ref } from 'vue'
import { mount } from '@vue/test-utils'

interface EndEvent {
    item: HTMLElement
    from: HTMLElement
    oldDraggableIndex?: number
    newDraggableIndex?: number
}

const sortable = vi.hoisted(() => ({
    options: null as null | {
        draggable: string
        handle: string
        disabled: boolean
        onEnd: (event: EndEvent) => void
    },
    element: null as HTMLElement | null,
    option: vi.fn(),
    destroy: vi.fn(),
}))

vi.mock('sortablejs', () => ({
    default: {
        create: vi.fn((element: HTMLElement, options: typeof sortable.options) => {
            sortable.element = element
            sortable.options = options
            return { option: sortable.option, destroy: sortable.destroy }
        }),
    },
}))

const { useSortableList } = await import('../useSortableList')

function mountList(initiallyDisabled = false) {
    const onMove = vi.fn()
    const disabled = ref(initiallyDisabled)
    const wrapper = mount(
        defineComponent({
            setup() {
                const container = ref<HTMLElement | null>(null)
                useSortableList(container, {
                    draggable: 'li',
                    handle: '.handle',
                    disabled,
                    onMove,
                })
                return () =>
                    h('ul', { ref: container }, [
                        h('li', { id: 'a' }, 'a'),
                        h('li', { id: 'b' }, 'b'),
                        h('li', { id: 'c' }, 'c'),
                    ])
            },
        }),
        { attachTo: document.body },
    )
    return { wrapper, onMove, disabled }
}

function ids(list: HTMLElement): string[] {
    return [...list.children].map((child) => child.id)
}

describe('useSortableList', () => {
    beforeEach(() => {
        sortable.options = null
        sortable.element = null
        sortable.option.mockReset()
        sortable.destroy.mockReset()
    })

    it('makes the container sortable by its handles', async () => {
        const { wrapper } = mountList()
        await nextTick()

        expect(sortable.element).toBe(wrapper.element)
        expect(sortable.options).toMatchObject({
            draggable: 'li',
            handle: '.handle',
            disabled: false,
        })

        wrapper.unmount()
        expect(sortable.destroy).toHaveBeenCalled()
    })

    it('puts a dropped element back and reports the move', async () => {
        const { wrapper, onMove } = mountList()
        await nextTick()
        const list = wrapper.element as HTMLElement
        const item = list.querySelector<HTMLElement>('#a')!

        // SortableJS moved a behind c.
        list.appendChild(item)
        sortable.options!.onEnd({ item, from: list, oldDraggableIndex: 0, newDraggableIndex: 2 })

        expect(ids(list)).toEqual(['a', 'b', 'c'])
        expect(onMove).toHaveBeenCalledWith(0, 2)
        wrapper.unmount()
    })

    it('puts an element moved up back at its old place', async () => {
        const { wrapper, onMove } = mountList()
        await nextTick()
        const list = wrapper.element as HTMLElement
        const item = list.querySelector<HTMLElement>('#c')!

        list.insertBefore(item, list.firstChild)
        sortable.options!.onEnd({ item, from: list, oldDraggableIndex: 2, newDraggableIndex: 0 })

        expect(ids(list)).toEqual(['a', 'b', 'c'])
        expect(onMove).toHaveBeenCalledWith(2, 0)
        wrapper.unmount()
    })

    it('reports nothing when the element stays at its place', async () => {
        const { wrapper, onMove } = mountList()
        await nextTick()
        const list = wrapper.element as HTMLElement
        const item = list.querySelector<HTMLElement>('#b')!

        sortable.options!.onEnd({ item, from: list, oldDraggableIndex: 1, newDraggableIndex: 1 })

        expect(onMove).not.toHaveBeenCalled()
        wrapper.unmount()
    })

    it('follows the disabled state', async () => {
        const { wrapper, disabled } = mountList(true)
        await nextTick()
        expect(sortable.options?.disabled).toBe(true)

        disabled.value = false
        await nextTick()

        expect(sortable.option).toHaveBeenCalledWith('disabled', false)
        wrapper.unmount()
    })
})
