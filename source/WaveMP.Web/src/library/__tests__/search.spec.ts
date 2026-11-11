import { describe, expect, it } from 'vitest'
import { maxQueryLength, normalizeQuery, queryText, searchLocation } from '../search'

describe('normalizeQuery', () => {
    it('trims and joins white space', () => {
        expect(normalizeQuery('  alpa   band ')).toBe('alpa band')
        expect(normalizeQuery(null)).toBe('')
        expect(normalizeQuery('   ')).toBe('')
    })

    it('cuts long texts', () => {
        expect(normalizeQuery('a'.repeat(maxQueryLength + 10))).toHaveLength(maxQueryLength)
    })
})

describe('queryText and searchLocation', () => {
    it('read the text of the route', () => {
        expect(queryText({ q: ' balad ' })).toBe('balad')
        expect(queryText({ q: ['first', 'second'] })).toBe('first')
        expect(queryText({})).toBe('')
    })

    it('open the search page with or without text', () => {
        expect(searchLocation(' balad ')).toEqual({ name: 'library-search', query: { q: 'balad' } })
        expect(searchLocation('  ')).toEqual({ name: 'library-search', query: {} })
    })
})
