import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseMeter from '../BaseMeter.vue'

describe('BaseMeter', () => {
    it('shows the share as runtime variable and for assistive technology', () => {
        const wrapper = mount(BaseMeter, {
            props: { label: 'Used space', value: 0.256, valueText: '512 MB of 2 GB used' },
        })

        const meter = wrapper.get('[data-part="meter"]')
        expect(meter.attributes('role')).toBe('meter')
        expect(meter.attributes('aria-label')).toBe('Used space')
        expect(meter.attributes('aria-valuemin')).toBe('0')
        expect(meter.attributes('aria-valuemax')).toBe('100')
        expect(meter.attributes('aria-valuenow')).toBe('26')
        expect(meter.attributes('aria-valuetext')).toBe('512 MB of 2 GB used')
        expect(meter.attributes('style')).toContain('--wmp-meter-value: 0.256')
        expect(wrapper.find('[data-part="meter-track"] [data-part="meter-fill"]').exists()).toBe(
            true,
        )
    })

    it('keeps the share between 0 and 1', () => {
        const full = mount(BaseMeter, { props: { label: 'Full', value: 1.7 } })
        const invalid = mount(BaseMeter, { props: { label: 'Invalid', value: Number.NaN } })

        expect(full.get('[data-part="meter"]').attributes('aria-valuenow')).toBe('100')
        expect(invalid.get('[data-part="meter"]').attributes('aria-valuenow')).toBe('0')
        expect(invalid.get('[data-part="meter"]').attributes('aria-valuetext')).toBeUndefined()
    })
})
