import { describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import { mount } from '@vue/test-utils'
import BaseSlider from '../BaseSlider.vue'
import {
    clamp,
    ratio,
    roundToStep,
    valueForKey,
    valueFromPointer,
    valueFromPointerVertical,
} from '../slider'

const volume = { min: 0, max: 1, step: 0.05, largeStep: 0.1 }
const seconds = { min: 0, max: 200, step: 1, largeStep: 10 }

describe('clamp', () => {
    it('keeps values within the range', () => {
        expect(clamp(-1, 0, 1)).toBe(0)
        expect(clamp(0.5, 0, 1)).toBe(0.5)
        expect(clamp(2, 0, 1)).toBe(1)
    })
})

describe('roundToStep', () => {
    it('rounds to the nearest step without floating point noise', () => {
        expect(roundToStep(0.33, volume)).toBe(0.35)
        expect(roundToStep(0.1 + 0.2, volume)).toBe(0.3)
        expect(roundToStep(12.4, seconds)).toBe(12)
    })

    it('stays within the range', () => {
        expect(roundToStep(1.2, volume)).toBe(1)
        expect(roundToStep(-3, seconds)).toBe(0)
        expect(roundToStep(Number.NaN, seconds)).toBe(0)
    })
})

describe('ratio', () => {
    it('maps the value to 0..1', () => {
        expect(ratio(50, seconds)).toBe(0.25)
        expect(ratio(250, seconds)).toBe(1)
        expect(ratio(5, { min: 5, max: 5 })).toBe(0)
    })
})

describe('valueFromPointer', () => {
    const rect = { left: 100, width: 200 }

    it('maps the pointer position to a value', () => {
        expect(valueFromPointer(100, rect, seconds)).toBe(0)
        expect(valueFromPointer(150, rect, seconds)).toBe(50)
        expect(valueFromPointer(300, rect, seconds)).toBe(200)
        expect(valueFromPointer(259, rect, volume)).toBe(0.8)
    })

    it('clamps positions outside the slider', () => {
        expect(valueFromPointer(0, rect, seconds)).toBe(0)
        expect(valueFromPointer(999, rect, seconds)).toBe(200)
        expect(valueFromPointer(150, { left: 0, width: 0 }, seconds)).toBe(0)
    })
})

describe('valueForKey', () => {
    it('moves by one step with the arrow keys', () => {
        expect(valueForKey('ArrowRight', 0.5, volume)).toBe(0.55)
        expect(valueForKey('ArrowUp', 0.5, volume)).toBe(0.55)
        expect(valueForKey('ArrowLeft', 0.5, volume)).toBe(0.45)
        expect(valueForKey('ArrowDown', 0.5, volume)).toBe(0.45)
    })

    it('moves by a large step with page up and page down', () => {
        expect(valueForKey('PageUp', 100, seconds)).toBe(110)
        expect(valueForKey('PageDown', 5, seconds)).toBe(0)
    })

    it('jumps to the ends with home and end', () => {
        expect(valueForKey('Home', 0.5, volume)).toBe(0)
        expect(valueForKey('End', 0.5, volume)).toBe(1)
    })

    it('ignores other keys', () => {
        expect(valueForKey('Enter', 0.5, volume)).toBeNull()
    })
})

describe('valueFromPointerVertical', () => {
    const rect = { top: 100, height: 240 }
    const gain = { min: -12, max: 12, step: 0.5, largeStep: 3 }

    it('maps the bottom to min and the top to max', () => {
        expect(valueFromPointerVertical(340, rect, gain)).toBe(-12)
        expect(valueFromPointerVertical(220, rect, gain)).toBe(0)
        expect(valueFromPointerVertical(100, rect, gain)).toBe(12)
        expect(valueFromPointerVertical(190, rect, gain)).toBe(3)
    })

    it('clamps positions outside the slider', () => {
        expect(valueFromPointerVertical(0, rect, gain)).toBe(12)
        expect(valueFromPointerVertical(999, rect, gain)).toBe(-12)
        expect(valueFromPointerVertical(150, { top: 0, height: 0 }, gain)).toBe(-12)
    })
})

describe('BaseSlider', () => {
    it('is horizontal by default', () => {
        const wrapper = mount(BaseSlider, { props: { modelValue: 0.5, label: 'Volume' } })

        expect(wrapper.get('[data-part="slider"]').attributes('data-orientation')).toBe(
            'horizontal',
        )
        expect(wrapper.get('[role="slider"]').attributes('aria-orientation')).toBe('horizontal')
    })

    it('follows the pointer from the bottom to the top when vertical', async () => {
        const wrapper = mount(BaseSlider, {
            props: {
                modelValue: 0,
                label: '1 kHz',
                min: -12,
                max: 12,
                step: 0.5,
                orientation: 'vertical',
            },
        })
        const slider = wrapper.get('[data-part="slider"]')
        slider.element.getBoundingClientRect = () =>
            ({ top: 0, left: 0, width: 20, height: 240 }) as DOMRect

        slider.element.dispatchEvent(
            new MouseEvent('pointerdown', { button: 0, clientX: 500, clientY: 60, bubbles: true }),
        )
        await nextTick()

        expect(slider.attributes('data-orientation')).toBe('vertical')
        expect(wrapper.get('[role="slider"]').attributes('aria-orientation')).toBe('vertical')
        expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual([6])
    })

    it('moves up with the arrow keys when vertical', async () => {
        const wrapper = mount(BaseSlider, {
            props: {
                modelValue: 0,
                label: '1 kHz',
                min: -12,
                max: 12,
                step: 0.5,
                orientation: 'vertical',
            },
        })

        await wrapper.get('[role="slider"]').trigger('keydown', { key: 'ArrowUp' })

        expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual([0.5])
    })
})
