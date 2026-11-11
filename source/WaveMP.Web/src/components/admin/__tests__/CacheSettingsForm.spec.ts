import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import CacheSettingsForm from '../CacheSettingsForm.vue'
import { createWaveMpI18n } from '@/i18n'
import { useCacheSettingsStore } from '@/stores/cacheSettings'

async function mountForm(maxSizeMegabytes = 2000) {
    const store = useCacheSettingsStore()
    const load = vi.spyOn(store, 'load').mockImplementation(async () => {
        store.settings = { maxSizeMegabytes }
        store.status = 'ready'
    })
    const update = vi.spyOn(store, 'update')
    const wrapper = mount(CacheSettingsForm, {
        global: { plugins: [createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, store, load, update }
}

describe('CacheSettingsForm', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows the saved limit among the offered sizes', async () => {
        const { wrapper } = await mountForm()

        expect((wrapper.get('select').element as HTMLSelectElement).value).toBe('2000')
        const options = wrapper.findAll('option').map((option) => option.text())
        expect(options).toEqual([
            '500 MB',
            '1 GB',
            '2 GB',
            '5 GB',
            '10 GB',
            '20 GB',
            '50 GB',
            '100 GB',
        ])
        wrapper.unmount()
    })

    it('offers a limit of the server that is not in the list', async () => {
        const { wrapper } = await mountForm(750)

        expect(wrapper.findAll('option').map((option) => option.text())).toContain('750 MB')
        expect((wrapper.get('select').element as HTMLSelectElement).value).toBe('750')
        wrapper.unmount()
    })

    it('saves the chosen limit and tells the page', async () => {
        const { wrapper, update } = await mountForm()
        update.mockResolvedValue({ maxSizeMegabytes: 5000 })

        await wrapper.get('select').setValue('5000')
        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(update).toHaveBeenCalledWith({ maxSizeMegabytes: 5000 })
        expect(wrapper.get('[data-part="alert"]').text()).toBe('The settings are saved.')
        expect(wrapper.emitted('saved')).toHaveLength(1)
        wrapper.unmount()
    })

    it('marks the field of a rejected limit', async () => {
        const { wrapper, update } = await mountForm()
        update.mockRejectedValue({
            responseStatusCode: 400,
            code: 'request.invalid',
            errors: [{ field: 'maxSizeMegabytes', code: 'out_of_range', min: 100, max: 1_000_000 }],
        })

        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(wrapper.get('[data-part="field"]').attributes('data-state')).toBe('invalid')
        expect(wrapper.get('[data-part="field-error"]').text()).toBe(
            'Choose a size between 100 MB and 1 TB.',
        )
        expect(wrapper.emitted('saved')).toBeUndefined()
        wrapper.unmount()
    })

    it('reports other errors', async () => {
        const { wrapper, update } = await mountForm()
        update.mockRejectedValue(new Error('offline'))

        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"]').attributes('data-state')).toBe('error')
        wrapper.unmount()
    })

    it('reports settings that cannot be loaded', async () => {
        const store = useCacheSettingsStore()
        vi.spyOn(store, 'load').mockImplementation(async () => {
            store.status = 'error'
        })

        const wrapper = mount(CacheSettingsForm, { global: { plugins: [createWaveMpI18n('en')] } })
        await flushPromises()

        expect(wrapper.find('form').exists()).toBe(false)
        expect(wrapper.get('[data-part="alert"]').text()).toBe('The settings could not be loaded.')
    })
})
