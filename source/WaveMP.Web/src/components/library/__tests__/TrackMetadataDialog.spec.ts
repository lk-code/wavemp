import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import type { TrackMetadata } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'

const api = vi.hoisted(() => ({
    getTrackMetadata: vi.fn(),
    updateTrackMetadata: vi.fn(),
}))

vi.mock('@/library/api', () => api)

const TrackMetadataDialog = (await import('../TrackMetadataDialog.vue')).default

const metadata: TrackMetadata = {
    trackId: 'song',
    status: 'Editable',
    audioFilePath: 'Band/Album/01 - Song.mp3',
    title: 'Song',
    artist: 'Band',
    album: 'Album',
    albumArtist: 'Band',
    year: 2001,
    trackNumber: 1,
    discNumber: null,
    genre: 'Rock',
}

async function mountDialog() {
    const wrapper = mount(TrackMetadataDialog, {
        props: { trackId: 'song', open: false },
        global: { plugins: [createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await wrapper.setProps({ open: true })
    await flushPromises()
    return wrapper
}

type Wrapper = Awaited<ReturnType<typeof mountDialog>>

function input(wrapper: Wrapper, field: string) {
    return wrapper.get(`[data-part="field"][data-field="${field}"] input`)
}

function value(wrapper: Wrapper, field: string): string {
    return (input(wrapper, field).element as HTMLInputElement).value
}

async function submit(wrapper: Wrapper) {
    await wrapper.get('form').trigger('submit')
    await flushPromises()
}

function saveButton(wrapper: Wrapper) {
    return wrapper.get('[data-part="dialog-footer"] button[type="submit"]')
}

describe('TrackMetadataDialog', () => {
    beforeEach(() => {
        api.getTrackMetadata.mockReset()
        api.updateTrackMetadata.mockReset()
        api.getTrackMetadata.mockResolvedValue(metadata)
    })

    it('loads the metadata into the fields and names the file', async () => {
        const wrapper = await mountDialog()

        expect(api.getTrackMetadata).toHaveBeenCalledWith('song')
        expect(wrapper.get('[data-part="dialog-title"]').text()).toBe('Edit metadata')
        expect(value(wrapper, 'title')).toBe('Song')
        expect(value(wrapper, 'artist')).toBe('Band')
        expect(value(wrapper, 'album')).toBe('Album')
        expect(value(wrapper, 'album-artist')).toBe('Band')
        expect(value(wrapper, 'year')).toBe('2001')
        expect(value(wrapper, 'number')).toBe('1')
        expect(value(wrapper, 'disc')).toBe('')
        expect(value(wrapper, 'genre')).toBe('Rock')
        expect(input(wrapper, 'year').attributes('inputmode')).toBe('numeric')
        expect(wrapper.findAll('[data-part="field-row"] [data-part="field"]')).toHaveLength(3)
        expect(wrapper.text()).toContain('The tags are written to Band/Album/01 - Song.mp3.')
        expect(saveButton(wrapper).attributes('disabled')).toBeUndefined()
        wrapper.unmount()
    })

    it.each([
        ['NoAudio', 'This song only has a video.'],
        ['NotWritable', 'The audio file cannot be written.'],
    ] as const)('disables everything with a hint for %s', async (status, hint) => {
        api.getTrackMetadata.mockResolvedValue({ ...metadata, status })

        const wrapper = await mountDialog()

        const inputs = wrapper.findAll('[data-part="form"] input')
        expect(inputs).toHaveLength(8)
        expect(inputs.every((item) => item.attributes('disabled') !== undefined)).toBe(true)
        expect(saveButton(wrapper).attributes('disabled')).toBeDefined()
        expect(wrapper.get('[data-part="alert"][data-state="info"]').text()).toContain(hint)
        const notes = wrapper.findAll('[data-part="form-note"]').map((note) => note.text())
        expect(notes.join(' ')).not.toContain('Empty fields')
        wrapper.unmount()
    })

    it('sends only the changed values and null for cleared fields', async () => {
        const saved = { ...metadata, title: 'New Title', genre: null, year: 1999 }
        api.updateTrackMetadata.mockResolvedValue(saved)
        const wrapper = await mountDialog()

        await input(wrapper, 'title').setValue('  New   Title ')
        await input(wrapper, 'genre').setValue('')
        await input(wrapper, 'year').setValue('1999')
        await submit(wrapper)

        expect(api.updateTrackMetadata).toHaveBeenCalledWith('song', {
            title: 'New Title',
            genre: null,
            year: 1999,
        })
        expect(wrapper.emitted('saved')).toEqual([[saved]])
        expect(wrapper.emitted('update:open')?.at(-1)).toEqual([false])
        wrapper.unmount()
    })

    it('closes without a request if nothing changed', async () => {
        const wrapper = await mountDialog()

        await input(wrapper, 'artist').setValue(' Band ')
        await submit(wrapper)

        expect(api.updateTrackMetadata).not.toHaveBeenCalled()
        expect(wrapper.emitted('update:open')?.at(-1)).toEqual([false])
        wrapper.unmount()
    })

    it('shows invalid numbers at their fields without a request', async () => {
        const wrapper = await mountDialog()

        await input(wrapper, 'number').setValue('0')
        await input(wrapper, 'disc').setValue('x')
        await submit(wrapper)

        expect(api.updateTrackMetadata).not.toHaveBeenCalled()
        const number = wrapper.get('[data-part="field"][data-field="number"]')
        expect(number.attributes('data-state')).toBe('invalid')
        expect(number.get('[data-part="field-error"]').text()).toBe(
            'Enter a whole number from 1 to 9999.',
        )
        expect(wrapper.get('[data-field="disc"]').attributes('data-state')).toBe('invalid')
        expect(wrapper.get('[data-field="year"]').attributes('data-state')).toBeUndefined()
        wrapper.unmount()
    })

    it('greys the fields out when the file turns out not to be writable', async () => {
        api.updateTrackMetadata.mockRejectedValue({
            code: 'track.not_writable',
            responseStatusCode: 409,
        })
        const wrapper = await mountDialog()

        await input(wrapper, 'title').setValue('Other')
        await submit(wrapper)

        expect(wrapper.get('[data-part="alert"][data-state="error"]').text()).toBe(
            'The changes could not be written to the audio file.',
        )
        expect(wrapper.get('[data-part="alert"][data-state="info"]').text()).toContain(
            'The audio file cannot be written.',
        )
        expect(input(wrapper, 'title').attributes('disabled')).toBeDefined()
        expect(saveButton(wrapper).attributes('disabled')).toBeDefined()
        expect(wrapper.emitted('saved')).toBeUndefined()
        wrapper.unmount()
    })

    it('marks the fields the server rejects', async () => {
        api.updateTrackMetadata.mockRejectedValue({
            code: 'request.invalid',
            responseStatusCode: 400,
            errors: [
                { field: 'title', code: 'max_length', max: 512 },
                { field: 'year', code: 'invalid_format' },
                { field: '', code: 'required' },
            ],
        })
        const wrapper = await mountDialog()

        await input(wrapper, 'title').setValue('Other')
        await submit(wrapper)

        expect(wrapper.get('[data-part="alert"][data-state="error"]').text()).toBe(
            'Please check the marked fields.',
        )
        expect(wrapper.get('[data-field="title"]').attributes('data-state')).toBe('invalid')
        expect(wrapper.get('[data-field="title"] [data-part="field-error"]').text()).toBe(
            'This text is too long (maximum length: 512).',
        )
        expect(wrapper.get('[data-field="year"]').attributes('data-state')).toBe('invalid')
        expect(wrapper.get('[data-field="year"] [data-part="field-error"]').text()).toBe(
            'This value has an invalid format.',
        )
        expect(wrapper.get('[data-field="artist"]').attributes('data-state')).toBeUndefined()
        wrapper.unmount()
    })

    it('reports other errors when saving', async () => {
        api.updateTrackMetadata.mockRejectedValue(new Error('offline'))
        const wrapper = await mountDialog()

        await input(wrapper, 'title').setValue('Other')
        await submit(wrapper)

        expect(wrapper.get('[data-part="alert"][data-state="error"]').text()).toBe(
            'The changes could not be saved.',
        )
        expect(input(wrapper, 'title').attributes('disabled')).toBeUndefined()
        wrapper.unmount()
    })

    it('offers to load again after an error', async () => {
        api.getTrackMetadata.mockRejectedValueOnce(new Error('offline'))
        const wrapper = await mountDialog()

        expect(wrapper.get('[data-part="alert"]').text()).toBe('The metadata could not be loaded.')
        expect(wrapper.find('form').exists()).toBe(false)
        await wrapper.get('[data-part="dialog-body"] button').trigger('click')
        await flushPromises()

        expect(api.getTrackMetadata).toHaveBeenCalledTimes(2)
        expect(value(wrapper, 'title')).toBe('Song')
        wrapper.unmount()
    })
})
