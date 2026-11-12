import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import EqualizerView from '../EqualizerView.vue'
import type { UpdateEqualizerSettings } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { getEqualizer } from '@/player/api'
import { fallbackEqualizerBands } from '@/player/equalizer'
import { useSessionStore } from '@/stores/session'

vi.mock('@/player/api', async (importOriginal) => ({
    ...(await importOriginal<typeof import('@/player/api')>()),
    getEqualizer: vi.fn(),
}))

const rock = [5, 4, 3, 1, -1, -1, 1, 3, 4, 5]

describe('EqualizerView', () => {
    let update: ReturnType<typeof vi.fn<(change: UpdateEqualizerSettings) => Promise<void>>>

    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(getEqualizer)
            .mockReset()
            .mockResolvedValue({
                minGainDb: -12,
                maxGainDb: 12,
                bands: fallbackEqualizerBands.map((band) => ({
                    frequencyHz: band.frequency,
                    filterType: band.type,
                    q: band.q,
                })),
                presets: [{ preset: 'Rock', gainsDb: rock }],
            })
        const session = useSessionStore()
        session.token = 'token'
        update = vi.fn((_change: UpdateEqualizerSettings) => Promise.resolve())
        session.updateEqualizer = update
    })

    function mountView() {
        return mount(EqualizerView, { global: { plugins: [createWaveMpI18n('en')] } })
    }

    it('loads the definition and shows the equalizer of the profile', async () => {
        const wrapper = mountView()
        await flushPromises()

        expect(getEqualizer).toHaveBeenCalledOnce()
        expect(wrapper.get('[data-part="view"]').attributes('data-view')).toBe('equalizer')
        expect(wrapper.findAll('[data-kind="band"]')).toHaveLength(10)
        // jsdom has no Web Audio.
        expect(wrapper.findAll('[data-part="alert"]').map((alert) => alert.text())).toEqual([
            expect.stringContaining('Web Audio'),
        ])
    })

    it('applies a preset and saves what is open when the page is left', async () => {
        const wrapper = mountView()
        await flushPromises()

        await wrapper.get('[data-field="equalizer-preset"] select').setValue('Rock')
        expect(wrapper.get('[data-frequency="31"] [data-part="equalizer-value"]').text()).toBe(
            '+5.0',
        )
        wrapper.unmount()
        await flushPromises()

        expect(update).toHaveBeenCalledWith({ preset: 'Rock' })
    })

    it('shows the default bands when the definition cannot be loaded', async () => {
        vi.mocked(getEqualizer).mockRejectedValue(new TypeError('Failed to fetch'))
        const wrapper = mountView()
        await flushPromises()

        expect(wrapper.findAll('[data-part="alert"]')[0]!.text()).toBe(
            'The equalizer could not be loaded. The default bands are shown.',
        )
        expect(wrapper.findAll('[data-kind="band"]')).toHaveLength(10)
    })

    it('reports a failed save', async () => {
        update.mockRejectedValueOnce(new TypeError('Failed to fetch'))
        const wrapper = mountView()
        await flushPromises()

        await wrapper.get('[data-item="power"]').trigger('click')
        wrapper.unmount()
        await flushPromises()
        const again = mountView()
        await flushPromises()

        expect(again.get('[data-part="alert"][data-state="error"]').text()).toBe(
            'The equalizer could not be saved.',
        )
    })
})
