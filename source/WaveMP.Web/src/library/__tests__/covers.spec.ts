import { describe, expect, it } from 'vitest'
import { coverSizeFor, coverSizes, defaultCoverSize } from '../covers'

describe('coverSizeFor', () => {
    it('rounds the shown width up to a size of the API', () => {
        expect(coverSizes).toEqual([96, 300, 1000])
        expect(coverSizeFor(40)).toBe(96)
        expect(coverSizeFor(96)).toBe(96)
        expect(coverSizeFor(97)).toBe(300)
        expect(coverSizeFor(180)).toBe(300)
        expect(coverSizeFor(400)).toBe(1000)
        expect(coverSizeFor(2400)).toBe(1000)
    })

    it('loads sharper covers on screens with a high pixel density, at most twice the width', () => {
        expect(coverSizeFor(48, 2)).toBe(96)
        expect(coverSizeFor(64, 2)).toBe(300)
        expect(coverSizeFor(160, 1)).toBe(300)
        expect(coverSizeFor(160, 2)).toBe(1000)
        expect(coverSizeFor(140, 3)).toBe(300)
        expect(coverSizeFor(48, 0.5)).toBe(96)
    })

    it('uses the default size while the width is unknown', () => {
        expect(defaultCoverSize).toBe(300)
        expect(coverSizeFor(0)).toBe(300)
        expect(coverSizeFor(Number.NaN)).toBe(300)
        expect(coverSizeFor(-10, 2)).toBe(300)
        expect(coverSizeFor(50, Number.NaN)).toBe(96)
    })
})
