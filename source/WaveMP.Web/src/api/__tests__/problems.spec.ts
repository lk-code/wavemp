import { describe, expect, it } from 'vitest'
import { isUnauthorized, problemCodeOf, statusOf, fieldErrorsOf } from '../problems'

describe('problemCodeOf', () => {
    it('returns known problem codes', () => {
        expect(problemCodeOf({ code: 'user.name_taken' })).toBe('user.name_taken')
        expect(problemCodeOf({ code: 'auth.unauthorized', responseStatusCode: 401 })).toBe(
            'auth.unauthorized',
        )
    })

    it('returns undefined for anything else', () => {
        expect(problemCodeOf({ code: 'unknown.code' })).toBeUndefined()
        expect(problemCodeOf({ code: 42 })).toBeUndefined()
        expect(problemCodeOf(new TypeError('Failed to fetch'))).toBeUndefined()
        expect(problemCodeOf(null)).toBeUndefined()
        expect(problemCodeOf(undefined)).toBeUndefined()
    })
})

describe('statusOf', () => {
    it('returns the status code of failed requests', () => {
        expect(statusOf({ responseStatusCode: 409 })).toBe(409)
    })

    it('returns undefined for network errors', () => {
        expect(statusOf(new TypeError('Failed to fetch'))).toBeUndefined()
        expect(statusOf(null)).toBeUndefined()
    })
})

describe('isUnauthorized', () => {
    it('detects a rejected session token', () => {
        expect(isUnauthorized({ responseStatusCode: 401 })).toBe(true)
        expect(isUnauthorized({ code: 'auth.unauthorized' })).toBe(true)
    })

    it('ignores other errors', () => {
        expect(isUnauthorized({ responseStatusCode: 403, code: 'auth.forbidden' })).toBe(false)
        expect(isUnauthorized(new TypeError('Failed to fetch'))).toBe(false)
    })
})

describe('fieldErrorsOf', () => {
    it('returns the fields of a validation problem by their JSON names', () => {
        const error = {
            code: 'request.invalid',
            errors: {
                additionalData: {
                    scanIntervalMinutes: ['Out of range'],
                    '$.audioExtensions': ['x'],
                },
            },
        }

        expect(fieldErrorsOf(error)).toEqual(['scanIntervalMinutes', 'audioExtensions'])
    })

    it('returns nothing for other errors', () => {
        expect(fieldErrorsOf(new Error('offline'))).toEqual([])
        expect(fieldErrorsOf(undefined)).toEqual([])
    })
})
