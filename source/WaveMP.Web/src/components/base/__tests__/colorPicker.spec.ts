import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseColorPicker from '../BaseColorPicker.vue'
import { colorTabStop, nextColorIndex } from '../colorPicker'

const options = [
    { value: '#1f6fd6', label: 'Blue' },
    { value: '#2e7d32', label: 'Green' },
    { value: '#c2185b', label: 'Pink' },
]

describe('nextColorIndex', () => {
    it('moves with the arrow keys and wraps around', () => {
        expect(nextColorIndex('ArrowRight', 0, 3)).toBe(1)
        expect(nextColorIndex('ArrowDown', 2, 3)).toBe(0)
        expect(nextColorIndex('ArrowLeft', 0, 3)).toBe(2)
        expect(nextColorIndex('ArrowUp', 2, 3)).toBe(1)
    })

    it('jumps to the first and last color', () => {
        expect(nextColorIndex('Home', 2, 3)).toBe(0)
        expect(nextColorIndex('End', 0, 3)).toBe(2)
    })

    it('starts at the ends without a selection', () => {
        expect(nextColorIndex('ArrowRight', -1, 3)).toBe(0)
        expect(nextColorIndex('ArrowLeft', -1, 3)).toBe(2)
    })

    it('ignores other keys and empty pickers', () => {
        expect(nextColorIndex('Enter', 0, 3)).toBeNull()
        expect(nextColorIndex('ArrowRight', -1, 0)).toBeNull()
    })
})

describe('colorTabStop', () => {
    it('is the selected swatch, otherwise the first', () => {
        expect(colorTabStop(2)).toBe(2)
        expect(colorTabStop(-1)).toBe(0)
    })
})

describe('BaseColorPicker', () => {
    it('renders the colors as a radio group', () => {
        const wrapper = mount(BaseColorPicker, {
            props: { label: 'Color', options, modelValue: '#2E7D32' },
        })

        const group = wrapper.get('[data-part="color-picker"]')
        const label = wrapper.get('[data-part="field"] > [data-part="label"]')
        expect(group.attributes('role')).toBe('radiogroup')
        expect(group.attributes('aria-labelledby')).toBe(label.attributes('id'))
        const swatches = wrapper.findAll('[data-part="color-swatch"]')
        expect(swatches.map((swatch) => swatch.attributes('aria-checked'))).toEqual([
            'false',
            'true',
            'false',
        ])
        expect(swatches.map((swatch) => swatch.attributes('tabindex'))).toEqual(['-1', '0', '-1'])
        expect(swatches[0]?.attributes('style')).toContain('--wmp-swatch-color: #1f6fd6')
        expect(swatches[0]?.attributes('title')).toBe('Blue')
        expect(swatches[0]?.text()).toBe('Blue')
    })

    it('selects a color by click and with the arrow keys', async () => {
        const wrapper = mount(BaseColorPicker, {
            props: { label: 'Color', options, modelValue: null },
            attachTo: document.body,
        })

        await wrapper.findAll('[data-part="color-swatch"]')[2]?.trigger('click')
        expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual(['#c2185b'])

        await wrapper.setProps({ modelValue: '#c2185b' })
        await wrapper.get('[data-part="color-picker"]').trigger('keydown', { key: 'ArrowRight' })
        expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual(['#1f6fd6'])
        wrapper.unmount()
    })
})
