import { describe, expect, it } from 'vitest'
import {
    isFirstStart,
    isSecondaryProfile,
    profileColorOptions,
    profileTileStates,
} from '../presentation'

const admin = { id: 'admin', isBuiltIn: true }
const anna = { id: 'anna', isBuiltIn: false }

describe('isSecondaryProfile', () => {
    it('lets the built-in admin step back as soon as there are other profiles', () => {
        expect(isSecondaryProfile(admin, [admin])).toBe(false)
        expect(isSecondaryProfile(admin, [anna, admin])).toBe(true)
        expect(isSecondaryProfile(anna, [anna, admin])).toBe(false)
    })
})

describe('profileTileStates', () => {
    it('combines the states of a tile', () => {
        expect(profileTileStates(anna, [anna, admin])).toBeUndefined()
        expect(profileTileStates(anna, [anna, admin], 'anna')).toBe('current')
        expect(profileTileStates(admin, [admin])).toBe('built-in')
        expect(profileTileStates(admin, [anna, admin], 'admin')).toBe('built-in secondary current')
    })
})

describe('isFirstStart', () => {
    it('is true while the built-in admin is the only profile', () => {
        expect(isFirstStart([admin])).toBe(true)
        expect(isFirstStart([anna, admin])).toBe(false)
        expect(isFirstStart([anna])).toBe(false)
        expect(isFirstStart([])).toBe(false)
    })
})

/** WCAG 2 contrast ratio between white and the color. */
function contrastToWhite(color: string): number {
    const channel = (start: number) => {
        const value = Number.parseInt(color.slice(start, start + 2), 16) / 255
        return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4
    }
    const luminance = 0.2126 * channel(1) + 0.7152 * channel(3) + 0.0722 * channel(5)
    return 1.05 / (luminance + 0.05)
}

describe('profileColorOptions', () => {
    it('are the eight colors of the server palette with names', () => {
        expect(profileColorOptions).toHaveLength(8)
        expect(profileColorOptions[0]?.value).toBe('#1f6fd6')
        for (const option of profileColorOptions) {
            expect(option.value).toMatch(/^#[0-9a-f]{6}$/)
            expect(option.labelKey).toMatch(/^adminUsers\.colors\.[a-z]+$/)
        }
    })

    it('keep white initials readable', () => {
        for (const option of profileColorOptions) {
            expect(contrastToWhite(option.value), option.value).toBeGreaterThanOrEqual(4.5)
        }
    })
})
