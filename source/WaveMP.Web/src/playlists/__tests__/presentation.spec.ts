import { describe, expect, it } from 'vitest'
import { PlaylistCoverKindObject } from '@/api/generated/models'
import {
    coverUploadProblem,
    coverViewOf,
    maxCoverUploadBytes,
    moveItem,
    nextSlide,
} from '../presentation'

describe('coverUploadProblem', () => {
    it('accepts JPEG, PNG and WebP pictures up to the limit', () => {
        for (const type of ['image/jpeg', 'image/png', 'image/webp']) {
            expect(coverUploadProblem({ type, size: 1024 })).toBeNull()
        }
        expect(coverUploadProblem({ type: 'image/png', size: maxCoverUploadBytes })).toBeNull()
    })

    it('refuses other types and larger pictures', () => {
        expect(coverUploadProblem({ type: 'image/gif', size: 10 })).toBe('type')
        expect(coverUploadProblem({ type: '', size: 10 })).toBe('type')
        expect(coverUploadProblem({ type: 'image/jpeg', size: maxCoverUploadBytes + 1 })).toBe(
            'size',
        )
    })
})

describe('moveItem', () => {
    const items = ['a', 'b', 'c', 'd']

    it('moves an item down and up', () => {
        expect(moveItem(items, 0, 2)).toEqual(['b', 'c', 'a', 'd'])
        expect(moveItem(items, 3, 1)).toEqual(['a', 'd', 'b', 'c'])
    })

    it('keeps duplicates apart', () => {
        expect(moveItem(['x', 'y', 'x'], 2, 0)).toEqual(['x', 'x', 'y'])
    })

    it('changes nothing for the same or invalid indexes and never changes the input', () => {
        expect(moveItem(items, 1, 1)).toEqual(items)
        expect(moveItem(items, -1, 2)).toEqual(items)
        expect(moveItem(items, 1, 4)).toEqual(items)
        expect(items).toEqual(['a', 'b', 'c', 'd'])
    })
})

describe('coverViewOf', () => {
    it('shows an own picture with its version', () => {
        expect(
            coverViewOf({ kind: PlaylistCoverKindObject.Custom, version: 'v1', trackIds: [] }),
        ).toEqual({ kind: 'custom', version: 'v1' })
    })

    it('shows the covers of tracks', () => {
        expect(
            coverViewOf({ kind: PlaylistCoverKindObject.Tracks, trackIds: ['t1', 't2'] }),
        ).toEqual({ kind: 'tracks', trackIds: ['t1', 't2'] })
    })

    it('falls back to the placeholder', () => {
        expect(coverViewOf(undefined)).toEqual({ kind: 'none' })
        expect(coverViewOf(null)).toEqual({ kind: 'none' })
        expect(coverViewOf({ kind: PlaylistCoverKindObject.None, trackIds: [] })).toEqual({
            kind: 'none',
        })
        expect(coverViewOf({ kind: PlaylistCoverKindObject.Tracks, trackIds: [] })).toEqual({
            kind: 'none',
        })
        expect(coverViewOf({ kind: PlaylistCoverKindObject.Custom, version: null })).toEqual({
            kind: 'none',
        })
    })
})

describe('nextSlide', () => {
    it('goes through the slides and starts over', () => {
        expect(nextSlide(0, 3)).toBe(1)
        expect(nextSlide(2, 3)).toBe(0)
        expect(nextSlide(0, 1)).toBe(0)
        expect(nextSlide(4, 0)).toBe(0)
    })
})
