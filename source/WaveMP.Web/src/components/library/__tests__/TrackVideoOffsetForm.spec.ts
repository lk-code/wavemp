import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createWaveMpI18n } from '@/i18n'

const api = vi.hoisted(() => ({
    getTrackVideoOffset: vi.fn(),
    setTrackVideoOffset: vi.fn(),
}))

vi.mock('@/library/api', () => api)

const TrackVideoOffsetForm = (await import('../TrackVideoOffsetForm.vue')).default

async function mountForm() {
    const wrapper = mount(TrackVideoOffsetForm, {
        props: { trackId: 'clip' },
        global: { plugins: [createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return wrapper
}

type Wrapper = Awaited<ReturnType<typeof mountForm>>

function input(wrapper: Wrapper) {
    return wrapper.get('[data-part="field"][data-field="video-offset"] input')
}

async function submit(wrapper: Wrapper, value: string) {
    await input(wrapper).setValue(value)
    await wrapper.get('form').trigger('submit')
    await flushPromises()
}

function error(wrapper: Wrapper): string | undefined {
    const field = wrapper.find('[data-part="field-error"]')
    return field.exists() ? field.text() : undefined
}

describe('TrackVideoOffsetForm', () => {
    beforeEach(() => {
        api.getTrackVideoOffset.mockReset().mockResolvedValue({
            trackId: 'clip',
            offsetMilliseconds: -1250,
        })
        api.setTrackVideoOffset
            .mockReset()
            .mockImplementation((id: string, offsetMilliseconds: number) =>
                Promise.resolve({ trackId: id, offsetMilliseconds }),
            )
    })

    it('shows the stored offset', async () => {
        const wrapper = await mountForm()

        expect(api.getTrackVideoOffset).toHaveBeenCalledWith('clip')
        expect((input(wrapper).element as HTMLInputElement).value).toBe('-1250')
        expect(wrapper.get('[data-part="field"] > [data-part="label"]').text()).toBe(
            'Video offset (milliseconds)',
        )
        wrapper.unmount()
    })

    it('saves a whole number and reports it', async () => {
        const wrapper = await mountForm()

        await submit(wrapper, '1500')

        expect(api.setTrackVideoOffset).toHaveBeenCalledWith('clip', 1500)
        expect(wrapper.emitted('saved')).toEqual([[1500]])
        expect(wrapper.get('[data-part="alert"]').text()).toBe('Saved.')
        wrapper.unmount()
    })

    it.each(['700000', '1.5', ''])('rejects %j before sending it', async (value) => {
        const wrapper = await mountForm()

        await submit(wrapper, value)

        expect(api.setTrackVideoOffset).not.toHaveBeenCalled()
        expect(error(wrapper)).toBe('Enter whole milliseconds from -600,000 to 600,000.')
        expect(wrapper.emitted('saved')).toBeUndefined()
        wrapper.unmount()
    })

    it('shows the limits the server rejects a value with', async () => {
        api.setTrackVideoOffset.mockRejectedValue({
            code: 'request.invalid',
            responseStatusCode: 400,
            errors: [{ field: 'offsetMilliseconds', code: 'out_of_range', min: -1000, max: 1000 }],
        })
        const wrapper = await mountForm()

        await submit(wrapper, '5000')

        expect(error(wrapper)).toBe('Enter whole milliseconds from -1,000 to 1,000.')
        wrapper.unmount()
    })

    it('reports other errors of saving and loading', async () => {
        api.setTrackVideoOffset.mockRejectedValue(new TypeError('Failed to fetch'))
        const wrapper = await mountForm()

        await submit(wrapper, '100')
        expect(wrapper.get('[data-part="alert"]').text()).toBe(
            'The video offset could not be saved.',
        )
        wrapper.unmount()

        api.getTrackVideoOffset.mockRejectedValue({
            code: 'track.no_video',
            responseStatusCode: 404,
        })
        const failed = await mountForm()
        expect(failed.get('[data-part="alert"]').text()).toBe('The video sync could not be loaded.')
        expect(failed.find('form').exists()).toBe(false)
        failed.unmount()
    })
})
