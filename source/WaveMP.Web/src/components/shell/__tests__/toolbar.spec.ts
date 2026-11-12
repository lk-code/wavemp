import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import AppToolbar from '../AppToolbar.vue'
import { searchDelay } from '@/composables/useSearchField'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'
import { usePageTitleStore } from '@/stores/pageTitle'

async function mountToolbar(path: string) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push(path)
    const wrapper = mount(AppToolbar, {
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, router }
}

describe('AppToolbar', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    it('searches the library from the search field', async () => {
        vi.useFakeTimers()
        const { wrapper, router } = await mountToolbar('/library/albums')

        await wrapper.get('[data-part="search"] input').setValue('balad')
        await vi.advanceTimersByTimeAsync(searchDelay + 10)
        await flushPromises()

        expect(router.currentRoute.value.fullPath).toBe('/library/search?q=balad')
        wrapper.unmount()
    })

    it('offers the sort options of the list and puts the choice into the route', async () => {
        const { wrapper, router } = await mountToolbar('/library/songs')

        const sort = wrapper.get('[data-part="sort"]')
        expect(sort.attributes('disabled')).toBeUndefined()
        await sort.trigger('click')
        await flushPromises()

        const items = wrapper.findAll('[data-part="menu-item"]')
        expect(items.map((item) => item.attributes('data-item'))).toEqual([
            'sort-album-artist',
            'sort-album',
            'sort-title',
            'sort-artist',
            'sort-duration',
            'sort-year',
            'sort-genre',
            'sort-first-seen-at',
            'sort-rating',
            'direction-asc',
            'direction-desc',
        ])
        expect(items[0]!.attributes('role')).toBe('menuitemradio')
        expect(items[0]!.attributes('aria-checked')).toBe('true')

        await wrapper.get('[data-item="sort-title"]').trigger('click')
        await flushPromises()
        expect(router.currentRoute.value.query).toEqual({ sort: 'Title' })

        await wrapper.get('[data-item="direction-desc"]').trigger('click')
        await flushPromises()
        expect(router.currentRoute.value.query).toEqual({ sort: 'Title', direction: 'Desc' })
        wrapper.unmount()
    })

    it('disables the sort options where nothing can be sorted', async () => {
        const { wrapper } = await mountToolbar('/library/recently-added')

        expect(wrapper.get('[data-part="sort"]').attributes('disabled')).toBeDefined()
        wrapper.unmount()
    })

    it('ends the breadcrumb of a detail page with its name', async () => {
        const { wrapper } = await mountToolbar('/library/albums/first')
        usePageTitleStore().set('First Album')
        await flushPromises()

        const labels = wrapper
            .findAll('[data-part="breadcrumb-link"] [data-part="label"]')
            .map((label) => label.text())
        expect(labels).toEqual(['Music', 'Library', 'Album', 'First Album'])
        wrapper.unmount()
    })
})
