import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import LibrarySettingsForm from '../LibrarySettingsForm.vue'
import { createWaveMpI18n } from '@/i18n'
import { useLibrarySettingsStore } from '@/stores/librarySettings'

const settings = {
    scanIntervalMinutes: 360,
    fileWatcherEnabled: true,
    audioExtensions: ['mp3', 'flac'],
    videoExtensions: ['mkv'],
    imageExtensions: ['jpg'],
    albumCoverNames: ['cover', 'folder'],
}

async function mountForm(current = settings) {
    const store = useLibrarySettingsStore()
    const load = vi.spyOn(store, 'load').mockImplementation(async () => {
        store.settings = current
        store.status = 'ready'
    })
    const update = vi.spyOn(store, 'update')
    const wrapper = mount(LibrarySettingsForm, {
        global: { plugins: [createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, store, load, update }
}

describe('LibrarySettingsForm', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows the saved settings', async () => {
        const { wrapper } = await mountForm()

        expect((wrapper.get('select').element as HTMLSelectElement).value).toBe('360')
        expect((wrapper.get('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(
            true,
        )
        const inputs = wrapper.findAll('input[data-part="input"]')
        expect(inputs.map((input) => (input.element as HTMLInputElement).value)).toEqual([
            'mp3, flac',
            'mkv',
            'jpg',
            'cover, folder',
        ])
        wrapper.unmount()
    })

    it('offers an interval of the server that is not in the list', async () => {
        const { wrapper } = await mountForm({ ...settings, scanIntervalMinutes: 45 })

        const options = wrapper.findAll('option').map((option) => option.text())
        expect(options).toContain('Every 45 minutes')
        wrapper.unmount()
    })

    it('saves the changes as lists and confirms', async () => {
        const { wrapper, update } = await mountForm()
        update.mockResolvedValue({
            ...settings,
            scanIntervalMinutes: 720,
            fileWatcherEnabled: false,
        })

        await wrapper.get('select').setValue('720')
        await wrapper.get('input[type="checkbox"]').setValue(false)
        await wrapper.findAll('input[data-part="input"]')[0]!.setValue(' .MP3 , ogg,')
        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(update).toHaveBeenCalledWith({
            scanIntervalMinutes: 720,
            fileWatcherEnabled: false,
            audioExtensions: ['.MP3', 'ogg'],
            videoExtensions: ['mkv'],
            imageExtensions: ['jpg'],
            albumCoverNames: ['cover', 'folder'],
        })
        expect(wrapper.get('[data-part="alert"][data-state="success"]').text()).toBe(
            'The settings are saved.',
        )
        wrapper.unmount()
    })

    it('shows the errors of the server at their fields', async () => {
        const { wrapper, update } = await mountForm()
        update.mockRejectedValue({
            responseStatusCode: 400,
            code: 'request.invalid',
            errors: [
                { field: 'scanIntervalMinutes', code: 'out_of_range', min: 15, max: 1440 },
                { field: 'videoExtensions', code: 'duplicate' },
                { field: 'albumCoverNames', code: 'max_items', max: 50 },
                { field: '', code: 'required' },
            ],
        })

        await wrapper.get('form').trigger('submit')
        await flushPromises()

        const errors = wrapper.findAll('[data-part="field-error"]').map((error) => error.text())
        expect(errors).toEqual([
            'Choose an interval between 15 and 1,440 minutes.',
            'An extension may only be in one list.',
            'This list has too many entries (maximum: 50).',
        ])
        expect(wrapper.find('[data-part="alert"]').exists()).toBe(false)
        wrapper.unmount()
    })

    it('reports other errors', async () => {
        const { wrapper, update } = await mountForm()
        update.mockRejectedValue(new Error('offline'))

        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"][data-state="error"]').text()).toBe(
            'The settings could not be saved.',
        )
        wrapper.unmount()
    })
})
