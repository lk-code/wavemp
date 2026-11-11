import { describe, expect, it } from 'vitest'
import { activeTreeId, buildBreadcrumb, pageTitleKey } from '../breadcrumb'

describe('buildBreadcrumb', () => {
    it('shows music, library and the section of the library', () => {
        const crumbs = buildBreadcrumb({ name: 'library-section', params: { section: 'songs' } })

        expect(crumbs).toEqual([
            { id: 'music', labelKey: 'breadcrumb.music', to: { name: 'library' } },
            { id: 'library', labelKey: 'nav.library', to: { name: 'library' } },
            { id: 'songs', labelKey: 'library.songs' },
        ])
    })

    it('shows the main pages below music', () => {
        expect(buildBreadcrumb({ name: 'now-playing', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'now-playing',
        ])
        expect(buildBreadcrumb({ name: 'playlists', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'playlists',
        ])
        expect(buildBreadcrumb({ name: 'settings', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'settings',
        ])
        expect(buildBreadcrumb({ name: 'not-found', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'not-found',
        ])
    })

    it('shows the pages of the administration below the administration', () => {
        for (const page of ['admin-library', 'admin-jobs', 'admin-users']) {
            const crumbs = buildBreadcrumb({ name: page, params: {} })
            expect(crumbs.map((c) => c.id)).toEqual(['music', 'admin', page])
            expect(crumbs[1]).toEqual({
                id: 'admin',
                labelKey: 'admin.title',
                to: { name: 'admin' },
            })
        }

        expect(pageTitleKey({ name: 'admin-library', params: {} })).toBe('adminLibrary.title')
        expect(pageTitleKey({ name: 'admin-jobs', params: {} })).toBe('adminJobs.title')
    })

    it('never links the current page', () => {
        const crumbs = buildBreadcrumb({ name: 'playlists', params: {} })

        expect(crumbs.at(-1)?.to).toBeUndefined()
        expect(buildBreadcrumb({ name: 'other', params: {} })).toEqual([
            { id: 'music', labelKey: 'breadcrumb.music' },
        ])
    })
})

describe('pageTitleKey', () => {
    it('is the label of the current page', () => {
        expect(pageTitleKey({ name: 'library-section', params: { section: 'albums' } })).toBe(
            'library.albums',
        )
        expect(pageTitleKey({ name: 'now-playing', params: {} })).toBe('nav.nowPlaying')
        expect(pageTitleKey({ name: 'settings', params: {} })).toBe('settings.title')
        expect(pageTitleKey({ name: 'admin-users', params: {} })).toBe('adminUsers.title')
    })
})

describe('activeTreeId', () => {
    it('selects the section of the library or the playlists', () => {
        expect(activeTreeId({ name: 'library-section', params: { section: 'years' } })).toBe(
            'years',
        )
        expect(activeTreeId({ name: 'playlists', params: {} })).toBe('playlists')
        expect(activeTreeId({ name: 'now-playing', params: {} })).toBeUndefined()
        expect(activeTreeId({ name: 'settings', params: {} })).toBeUndefined()
        expect(
            activeTreeId({ name: 'library-section', params: { section: 'unknown' } }),
        ).toBeUndefined()
    })
})

describe('detail pages', () => {
    it('end the breadcrumb with the name below their section', () => {
        const crumbs = buildBreadcrumb(
            { name: 'library-album', params: { id: 'a' } },
            'First Album',
        )

        expect(crumbs).toEqual([
            { id: 'music', labelKey: 'breadcrumb.music', to: { name: 'library' } },
            { id: 'library', labelKey: 'nav.library', to: { name: 'library' } },
            {
                id: 'albums',
                labelKey: 'library.albums',
                to: { name: 'library-section', params: { section: 'albums' } },
            },
            { id: 'library-album', labelKey: 'library.albums', label: 'First Album' },
        ])
    })

    it('end with their section while the name is unknown', () => {
        const crumbs = buildBreadcrumb({ name: 'library-genre', params: { genre: 'Rock' } })

        expect(crumbs.at(-1)).toEqual({ id: 'genres', labelKey: 'library.genres' })
        expect(pageTitleKey({ name: 'library-track', params: { id: 't' } })).toBe('library.songs')
    })

    it('select the tree node of their section', () => {
        expect(activeTreeId({ name: 'library-album', params: {} })).toBe('albums')
        expect(activeTreeId({ name: 'library-artist', params: {} })).toBe('artists')
        expect(activeTreeId({ name: 'library-genre', params: {} })).toBe('genres')
        expect(activeTreeId({ name: 'library-year', params: {} })).toBe('years')
        expect(activeTreeId({ name: 'library-track', params: {} })).toBe('songs')
        expect(activeTreeId({ name: 'library-search', params: {} })).toBeUndefined()
    })

    it('show the search below the library', () => {
        expect(buildBreadcrumb({ name: 'library-search', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'library',
            'search',
        ])
    })
})
