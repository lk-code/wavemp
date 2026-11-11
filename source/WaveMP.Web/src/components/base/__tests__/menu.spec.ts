import { describe, expect, it } from 'vitest'
import { clampMenuPosition, firstEnabledIndex, lastEnabledIndex, nextEnabledIndex } from '../menu'

const entries = [{ disabled: true }, {}, { separator: true }, {}, { disabled: true }]

describe('nextEnabledIndex', () => {
    it('skips disabled entries and separators', () => {
        expect(nextEnabledIndex(1, 1, entries)).toBe(3)
        expect(nextEnabledIndex(3, -1, entries)).toBe(1)
    })

    it('wraps around at the ends', () => {
        expect(nextEnabledIndex(3, 1, entries)).toBe(1)
        expect(nextEnabledIndex(1, -1, entries)).toBe(3)
    })

    it('returns -1 if no entry can be focused', () => {
        expect(nextEnabledIndex(0, 1, [{ disabled: true }, { separator: true }])).toBe(-1)
        expect(nextEnabledIndex(0, 1, [])).toBe(-1)
    })

    it('finds the first and the last focusable entry', () => {
        expect(firstEnabledIndex(entries)).toBe(1)
        expect(lastEnabledIndex(entries)).toBe(3)
    })
})

describe('clampMenuPosition', () => {
    const size = { width: 200, height: 100 }
    const viewport = { width: 800, height: 600 }

    it('keeps the point if the menu fits', () => {
        expect(clampMenuPosition({ x: 100, y: 100 }, size, viewport)).toEqual({ x: 100, y: 100 })
    })

    it('moves the menu back into the viewport', () => {
        expect(clampMenuPosition({ x: 750, y: 580 }, size, viewport)).toEqual({ x: 596, y: 496 })
        expect(clampMenuPosition({ x: -20, y: 2 }, size, viewport)).toEqual({ x: 4, y: 4 })
    })

    it('keeps the margin if the menu is larger than the viewport', () => {
        expect(clampMenuPosition({ x: 50, y: 50 }, { width: 900, height: 700 }, viewport)).toEqual({
            x: 4,
            y: 4,
        })
    })
})
