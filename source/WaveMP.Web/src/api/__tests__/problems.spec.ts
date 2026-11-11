import { describe, expect, it } from 'vitest'
import {
    fieldErrorsOf,
    isUnauthorized,
    memberOf,
    problemCodeOf,
    statusOf,
    validationErrorsOf,
} from '../problems'

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

describe('validationErrorsOf', () => {
    it('returns field, code and limits of a validation problem', () => {
        const error = {
            code: 'request.invalid',
            errors: [
                { field: 'scanIntervalMinutes', code: 'out_of_range', min: 15, max: 1440 },
                { field: '', code: 'required', min: null, max: null },
            ],
        }

        expect(validationErrorsOf(error)).toEqual([
            { field: 'scanIntervalMinutes', code: 'out_of_range', min: 15, max: 1440 },
            { field: '', code: 'required' },
        ])
    })

    it('treats unknown codes as invalid values and skips errors without a field', () => {
        const error = { errors: [{ field: 'name', code: 'too_fancy' }, { code: 'required' }, null] }

        expect(validationErrorsOf(error)).toEqual([{ field: 'name', code: 'invalid_value' }])
    })

    it('returns nothing for other errors', () => {
        expect(validationErrorsOf(new Error('offline'))).toEqual([])
        expect(validationErrorsOf({ errors: { name: ['Required'] } })).toEqual([])
        expect(validationErrorsOf(undefined)).toEqual([])
    })
})

describe('memberOf', () => {
    it('returns the top-level member of a field', () => {
        expect(memberOf('audioExtensions[2]')).toBe('audioExtensions')
        expect(memberOf('name.first')).toBe('name')
        expect(memberOf('year')).toBe('year')
        expect(memberOf('')).toBe('')
    })
})

describe('fieldErrorsOf', () => {
    it('returns the members of a validation problem with errors, each once', () => {
        const error = {
            code: 'request.invalid',
            errors: [
                { field: 'audioExtensions[1]', code: 'invalid_format' },
                { field: 'audioExtensions', code: 'max_items', max: 50 },
                { field: 'scanIntervalMinutes', code: 'out_of_range', min: 15, max: 1440 },
            ],
        }

        expect(fieldErrorsOf(error)).toEqual(['audioExtensions', 'scanIntervalMinutes'])
    })

    it('returns nothing for other errors', () => {
        expect(fieldErrorsOf(new Error('offline'))).toEqual([])
        expect(fieldErrorsOf(undefined)).toEqual([])
    })
})
