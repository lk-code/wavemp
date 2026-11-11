import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'
import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter, type Router } from 'vue-router'
import { searchDelay, useSearchField } from '../useSearchField'

const page = defineComponent({ render: () => h('div') })
const routes = [
    { path: '/library/search', name: 'library-search', component: page },
    { path: '/library/:section', name: 'library-section', component: page },
]

async function mountField(path: string) {
    const router = createRouter({ history: createMemoryHistory(), routes })
    await router.push(path)
    let field: ReturnType<typeof useSearchField> | null = null
    const wrapper = mount(
        defineComponent({
            setup() {
                field = useSearchField()
                return () => h('div')
            },
        }),
        { global: { plugins: [router] } },
    )
    return { router, wrapper, field: field! }
}

function location(router: Router): string {
    return router.currentRoute.value.fullPath
}

describe('useSearchField', () => {
    beforeEach(() => {
        vi.useFakeTimers()
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    it('opens the search page after a pause in typing', async () => {
        const { router, wrapper, field } = await mountField('/library/albums')

        field.text.value = ' alpa '
        await nextTick()
        await vi.advanceTimersByTimeAsync(searchDelay - 50)
        expect(location(router)).toBe('/library/albums')

        await vi.advanceTimersByTimeAsync(100)
        await flushPromises()
        expect(location(router)).toBe('/library/search?q=alpa')
        wrapper.unmount()
    })

    it('searches at once on Enter and replaces the entry on the search page', async () => {
        const { router, wrapper, field } = await mountField('/library/songs')
        const replace = vi.spyOn(router, 'replace')

        field.submit('first')
        await flushPromises()
        field.submit('second')
        await flushPromises()

        expect(location(router)).toBe('/library/search?q=second')
        expect(replace).toHaveBeenCalledTimes(1)
        wrapper.unmount()
    })

    it('shows the text of the search page and clears it elsewhere', async () => {
        const { router, wrapper, field } = await mountField('/library/search?q=balad')
        expect(field.text.value).toBe('balad')

        await router.push('/library/albums')
        await nextTick()
        expect(field.text.value).toBe('')

        await vi.advanceTimersByTimeAsync(searchDelay * 2)
        await flushPromises()
        expect(location(router)).toBe('/library/albums')
        wrapper.unmount()
    })

    it('does nothing for an empty text outside the search page', async () => {
        const { router, wrapper, field } = await mountField('/library/songs')

        field.submit('   ')
        await flushPromises()

        expect(location(router)).toBe('/library/songs')
        wrapper.unmount()
    })
})
