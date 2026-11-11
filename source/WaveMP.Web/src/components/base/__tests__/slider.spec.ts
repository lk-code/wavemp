import { describe, expect, it } from 'vitest'
import { clamp, ratio, roundToStep, valueForKey, valueFromPointer } from '../slider'

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
