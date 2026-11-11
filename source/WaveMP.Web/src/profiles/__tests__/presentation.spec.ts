import { describe, expect, it } from 'vitest'
import { isFirstStart, isSecondaryProfile, profileTileStates } from '../presentation'

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
