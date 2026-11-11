import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, ref, type Ref } from 'vue'
import { mount } from '@vue/test-utils'
import { useInView } from '../useInView'

function mountProbe() {
    const probe: { visible: Ref<boolean> | null } = { visible: null }
    const wrapper = mount(
        defineComponent({
            setup() {
                const element = ref<HTMLElement | null>(null)
                probe.visible = useInView(element)
                return () => h('div', { ref: element })
            },
        }),
    )
    return { wrapper, visible: () => probe.visible?.value }
}

describe('useInView', () => {
    afterEach(() => {
        vi.unstubAllGlobals()
    })

    it('counts the element as visible without IntersectionObserver', () => {
        vi.stubGlobal('IntersectionObserver', undefined)

        const { visible, wrapper } = mountProbe()

        expect(visible()).toBe(true)
        wrapper.unmount()
    })

    it('follows the intersection of the element and disconnects on unmount', async () => {
        const disconnect = vi.fn()
        let report: (entries: { isIntersecting: boolean }[]) => void = () => {}
        vi.stubGlobal(
            'IntersectionObserver',
            class {
                constructor(callback: (entries: { isIntersecting: boolean }[]) => void) {
                    report = callback
                }

                observe = vi.fn()
                disconnect = disconnect
            },
        )

        const { visible, wrapper } = mountProbe()
        await nextTick()
        expect(visible()).toBe(false)

        report([{ isIntersecting: true }])
        expect(visible()).toBe(true)

        wrapper.unmount()
        expect(disconnect).toHaveBeenCalled()
    })
})
