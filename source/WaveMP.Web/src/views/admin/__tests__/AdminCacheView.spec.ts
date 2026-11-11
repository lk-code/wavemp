import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import AdminCacheView from '../AdminCacheView.vue'
import type { CacheUsage } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'
import { useCacheStore } from '@/stores/cache'
import { useCacheSettingsStore } from '@/stores/cacheSettings'

const usage: CacheUsage = {
    isAvailable: true,
    sizeBytes: 1_000_000_000,
    entryCount: 40,
    maxSizeBytes: 2_000_000_000,
    covers: { sizeBytes: 1_000_000_000, entryCount: 40 },
    transcodes: { sizeBytes: 0, entryCount: 0 },
}

async function mountView(current: CacheUsage | null = usage) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push('/admin/cache')
    const cache = useCacheStore()
    const load = vi.spyOn(cache, 'load').mockImplementation(async () => {
        cache.usage = current
        cache.status = current ? 'ready' : 'error'
    })
    const clear = vi.spyOn(cache, 'clear')
    const settings = useCacheSettingsStore()
    vi.spyOn(settings, 'load').mockImplementation(async () => {
        settings.settings = { maxSizeMegabytes: 2000 }
        settings.status = 'ready'
    })
    const wrapper = mount(AdminCacheView, {
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, load, clear }
}

describe('AdminCacheView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows the usage and the settings of the cache', async () => {
        const { wrapper } = await mountView()

        expect(wrapper.get('[data-part="view"]').attributes('data-view')).toBe('admin-cache')
        expect(wrapper.get('[data-part="admin-nav-item"][aria-current="page"]').text()).toBe(
            'Cache',
        )
        const sections = wrapper.findAll('section[data-part="section"]')
        expect(sections.map((section) => section.attributes('data-item'))).toEqual([
            'usage',
            'settings',
        ])
        expect(wrapper.get('[data-part="meter"]').attributes('aria-valuenow')).toBe('50')
        expect(wrapper.find('select').exists()).toBe(true)
        expect(wrapper.find('[data-part="alert"]').exists()).toBe(false)
        wrapper.unmount()
    })

    it('clears the cache after asking', async () => {
        const { wrapper, clear } = await mountView()
        clear.mockResolvedValue()

        await wrapper.get('[data-part="cache-usage"] button').trigger('click')
        expect(wrapper.get('[data-part="dialog-title"]').text()).toBe('Clear cache')
        const confirm = wrapper
            .findAll('[data-part="dialog-footer"] button')
            .find((button) => button.text() === 'Clear')
        await confirm!.trigger('click')
        await flushPromises()

        expect(clear).toHaveBeenCalledTimes(1)
        expect(wrapper.get('[data-part="alert"]').text()).toBe('The cache is empty.')
        wrapper.unmount()
    })

    it('reports a cache that could not be cleared', async () => {
        const { wrapper, clear } = await mountView()
        clear.mockRejectedValue(new Error('offline'))

        await wrapper.get('[data-part="cache-usage"] button').trigger('click')
        const confirm = wrapper
            .findAll('[data-part="dialog-footer"] button')
            .find((button) => button.text() === 'Clear')
        await confirm!.trigger('click')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"]').attributes('data-state')).toBe('error')
        expect(wrapper.get('[data-part="alert"]').text()).toBe('The cache could not be cleared.')
        wrapper.unmount()
    })

    it('warns when the cache folder cannot be written', async () => {
        const { wrapper } = await mountView({ ...usage, isAvailable: false })

        expect(wrapper.get('[data-part="alert"]').text()).toContain(
            'The cache folder cannot be written',
        )
        wrapper.unmount()
    })

    it('reports a usage that could not be loaded', async () => {
        const { wrapper } = await mountView(null)

        expect(wrapper.find('[data-part="cache-usage"]').exists()).toBe(false)
        expect(wrapper.get('[data-part="alert"]').text()).toBe('The cache could not be loaded.')
        wrapper.unmount()
    })

    it('loads the usage again after the limit was saved', async () => {
        const { wrapper, load } = await mountView()
        const settings = useCacheSettingsStore()
        vi.spyOn(settings, 'update').mockResolvedValue({ maxSizeMegabytes: 5000 })

        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(load).toHaveBeenCalledTimes(2)
        wrapper.unmount()
    })
})
