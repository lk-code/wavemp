import { describe, expect, it } from 'vitest'
import {
    defaultDirection,
    defaultSort,
    effectiveSort,
    parseSort,
    sortContext,
    sortItemId,
    sortLabelKey,
    sortOptions,
} from '../sorting'

describe('sortOptions and defaults', () => {
    it('offers the relevance only with a search text', () => {
        expect(sortOptions('albums', false)).toEqual(['Artist', 'Title', 'Year', 'FirstSeenAt'])
        expect(sortOptions('albums', true)).toContain('Relevance')
        expect(sortOptions('genres', true)).toEqual(['Name', 'TrackCount'])
        expect(sortOptions('years', false)).toEqual([])
    })

    it('uses the defaults of the server', () => {
        expect(defaultSort('tracks', false)).toBe('AlbumArtist')
        expect(defaultSort('tracks', true)).toBe('Relevance')
        expect(defaultSort('artists', false)).toBe('Name')
        expect(defaultSort('genres', true)).toBe('Name')
        expect(defaultSort('years', false)).toBeUndefined()
    })

    it('descends for recent, relevant, counts and years', () => {
        expect(defaultDirection('tracks', 'Title')).toBe('Asc')
        expect(defaultDirection('tracks', 'FirstSeenAt')).toBe('Desc')
        expect(defaultDirection('albums', 'Relevance')).toBe('Desc')
        expect(defaultDirection('genres', 'TrackCount')).toBe('Desc')
        expect(defaultDirection('years', undefined)).toBe('Desc')
    })
})

describe('parseSort', () => {
    it('reads valid values and ignores unknown ones', () => {
        expect(parseSort('tracks', { sort: 'Title', direction: 'Desc' }, false)).toEqual({
            sort: 'Title',
            direction: 'Desc',
        })
        expect(parseSort('tracks', { sort: 'Size', direction: 'Up' }, false)).toEqual({
            sort: undefined,
            direction: undefined,
        })
        expect(parseSort('tracks', { sort: 'Relevance' }, false).sort).toBeUndefined()
        expect(parseSort('tracks', { sort: ['Year', 'Title'] }, true).sort).toBe('Year')
    })

    it('fills in the defaults', () => {
        expect(effectiveSort('tracks', {}, false)).toEqual({
            sort: 'AlbumArtist',
            direction: 'Asc',
        })
        expect(effectiveSort('tracks', {}, true)).toEqual({ sort: 'Relevance', direction: 'Desc' })
        expect(effectiveSort('albums', { direction: 'Desc' }, false)).toEqual({
            sort: 'Artist',
            direction: 'Desc',
        })
    })
})

describe('sortContext', () => {
    it('knows the sortable lists', () => {
        expect(sortContext({ name: 'library-section', params: { section: 'songs' } })).toBe(
            'tracks',
        )
        expect(sortContext({ name: 'library-section', params: { section: 'years' } })).toBe('years')
        expect(sortContext({ name: 'library-genre', params: { genre: 'Rock' } })).toBe('tracks')
        expect(sortContext({ name: 'library-year', params: { year: '2001' } })).toBe('tracks')
    })

    it('has nothing to sort on fixed lists and detail pages', () => {
        expect(
            sortContext({ name: 'library-section', params: { section: 'recently-added' } }),
        ).toBeNull()
        for (const name of ['library-search', 'library-album', 'library-artist', 'library-track']) {
            expect(sortContext({ name, params: {} }), name).toBeNull()
        }
    })
})

describe('sortItemId and sortLabelKey', () => {
    it('derive the id and the message key from the key', () => {
        expect(sortItemId('AlbumArtist')).toBe('sort-album-artist')
        expect(sortItemId('FirstSeenAt')).toBe('sort-first-seen-at')
        expect(sortItemId('Title')).toBe('sort-title')
        expect(sortLabelKey('AlbumArtist')).toBe('sort.keys.albumArtist')
        expect(sortLabelKey('TrackCount')).toBe('sort.keys.trackCount')
        expect(sortLabelKey('Name')).toBe('sort.keys.name')
    })
})
