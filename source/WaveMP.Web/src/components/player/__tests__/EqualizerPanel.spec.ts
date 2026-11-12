import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import EqualizerPanel from '../EqualizerPanel.vue'
import type { EqualizerPreset } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { fallbackEqualizerBands } from '@/player/equalizer'

const rock = [5, 4, 3, 1, -1, -1, 1, 3, 4, 5]

function mountPanel(
    props: Partial<{
        enabled: boolean
        preset: EqualizerPreset
        preampDb: number
        gainsDb: number[]
        available: boolean
    }> = {},
    locale: 'en' | 'de' = 'en',
) {
    return mount(EqualizerPanel, {
        props: {
            enabled: true,
            preset: 'Rock',
            preampDb: -2.5,
            gainsDb: rock,
            bands: fallbackEqualizerBands,
            minGainDb: -12,
            maxGainDb: 12,
            ...props,
        },
        global: { plugins: [createWaveMpI18n(locale)] },
    })
}

describe('EqualizerPanel', () => {
    it('shows the preamp, the scale and one vertical slider per band', () => {
        const wrapper = mountPanel()

        expect(wrapper.get('[data-part="equalizer"]').attributes('data-state')).toBe('on')
        const bands = wrapper.findAll('[data-part="equalizer-band"]')
        expect(bands.map((band) => band.attributes('data-kind'))).toEqual([
            'preamp',
            ...Array<string>(10).fill('band'),
        ])
        expect(bands.slice(1).map((band) => band.attributes('data-frequency'))).toEqual([
            '31',
            '62',
            '125',
            '250',
            '500',
            '1000',
            '2000',
            '4000',
            '8000',
            '16000',
        ])
        expect(bands.map((band) => band.get('[data-part="label"]').text())).toEqual([
            'Preamp',
            '31 Hz',
            '62 Hz',
            '125 Hz',
            '250 Hz',
            '500 Hz',
            '1 kHz',
            '2 kHz',
            '4 kHz',
            '8 kHz',
            '16 kHz',
        ])
        expect(bands.map((band) => band.get('[data-part="equalizer-value"]').text())).toEqual([
            '-2.5',
            '+5.0',
            '+4.0',
            '+3.0',
            '+1.0',
            '-1.0',
            '-1.0',
            '+1.0',
            '+3.0',
            '+4.0',
            '+5.0',
        ])
        const thumb = bands[6]!.get('[role="slider"]')
        expect(thumb.attributes('aria-orientation')).toBe('vertical')
        expect(thumb.attributes('aria-valuetext')).toBe('-1.0 dB')
        expect(thumb.attributes('aria-label')).toBe('1 kHz')
        expect(
            wrapper
                .findAll('[data-part="equalizer-scale"] [data-part="label"]')
                .map((l) => l.text()),
        ).toEqual(['+12 dB', '0 dB', '-12 dB'])
    })

    it('shows the controls with the preset and the power state', () => {
        const wrapper = mountPanel({ enabled: false, preset: 'Classical' })

        expect(wrapper.get('[data-part="equalizer"]').attributes('data-state')).toBe('off')
        const power = wrapper.get('[data-item="power"]')
        expect(power.attributes('aria-pressed')).toBe('false')
        const select = wrapper.get<HTMLSelectElement>(
            '[data-field="equalizer-preset"] [data-part="select"]',
        )
        expect(select.element.value).toBe('Classical')
        expect(select.findAll('option').map((option) => option.text())).toEqual([
            'Flat',
            'Rock',
            'Pop',
            'Jazz',
            'Classical',
            'Dance',
            'Bass',
            'Treble',
            'Vocal',
            'Custom',
        ])
        expect(wrapper.find('[data-part="alert"]').exists()).toBe(false)
    })

    it('uses German texts and number formats', () => {
        const wrapper = mountPanel({ preset: 'Classical', preampDb: 1.5 }, 'de')

        expect(
            wrapper.get('[data-field="equalizer-preset"] option[value="Classical"]').text(),
        ).toBe('Klassik')
        expect(wrapper.get('[data-kind="preamp"] [data-part="equalizer-value"]').text()).toBe(
            '+1,5',
        )
        expect(wrapper.get('[data-frequency="16000"] [data-part="label"]').text()).toBe('16 kHz')
    })

    it('emits the changes', async () => {
        const wrapper = mountPanel()

        await wrapper.get('[data-item="power"]').trigger('click')
        await wrapper.get('[data-field="equalizer-preset"] select').setValue('Pop')
        await wrapper.get('[data-item="reset"]').trigger('click')
        await wrapper
            .get('[data-frequency="1000"] [role="slider"]')
            .trigger('keydown', { key: 'ArrowUp' })
        await wrapper
            .get('[data-kind="preamp"] [role="slider"]')
            .trigger('keydown', { key: 'PageDown' })

        expect(wrapper.emitted('enable')).toEqual([[false]])
        expect(wrapper.emitted('preset')).toEqual([['Pop']])
        expect(wrapper.emitted('reset')).toHaveLength(1)
        expect(wrapper.emitted('band')).toEqual([[5, -0.5]])
        expect(wrapper.emitted('preamp')).toEqual([[-5.5]])
    })

    it('says when the browser cannot apply the equalizer', () => {
        const wrapper = mountPanel({ available: false })

        expect(wrapper.get('[data-part="alert"]').attributes('data-state')).toBe('info')
        expect(wrapper.get('[data-part="alert"]').text()).toContain('Web Audio')
    })
})
