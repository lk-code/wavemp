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
        expect(buildBreadcrumb({ name: 'admin-users', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'admin-users',
        ])
        expect(buildBreadcrumb({ name: 'not-found', params: {} }).map((c) => c.id)).toEqual([
            'music',
            'not-found',
        ])
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
