import { describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { createRoutes, devRoutes } from '../routes'

async function resolve(path: string, routes = createRoutes()) {
    const router = createRouter({ history: createMemoryHistory(), routes })
    await router.push(path)
    return router.currentRoute.value
}

describe('createRoutes', () => {
    it('ends with the catch-all route for unknown pages', () => {
        const routes = createRoutes([{ path: '/extra', name: 'extra', redirect: '/' }])

        expect(routes.at(-1)?.name).toBe('not-found')
        expect(routes.at(-2)?.name).toBe('extra')
    })

    it('opens the home page on the start page', async () => {
        const route = await resolve('/')

        expect(route.name).toBe('home')
        expect(route.fullPath).toBe('/')
        expect(route.meta.tab).toBe('home')
    })

    it('redirects the library to its songs', async () => {
        expect((await resolve('/library')).fullPath).toBe('/library/songs')
    })

    it('knows the sections of the library', async () => {
        for (const section of [
            'recently-added',
            'favorites',
            'artists',
            'albums',
            'songs',
            'genres',
            'years',
        ]) {
            const route = await resolve(`/library/${section}`)
            expect(route.name).toBe('library-section')
            expect(route.params.section).toBe(section)
        }
    })

    it('knows the search and the detail pages of the library', async () => {
        for (const [path, name, param] of [
            ['/library/search?q=balad', 'library-search', undefined],
            ['/library/albums/a1', 'library-album', 'a1'],
            ['/library/artists/b2', 'library-artist', 'b2'],
            ['/library/genres/Hip%20Hop', 'library-genre', 'Hip Hop'],
            ['/library/years/2001', 'library-year', '2001'],
            ['/library/tracks/c3', 'library-track', 'c3'],
        ] as const) {
            const route = await resolve(path)
            expect(route.name, path).toBe(name)
            expect(route.meta.tab, path).toBe('library')
            if (param) {
                expect(Object.values(route.params), path).toEqual([param])
            }
        }
    })

    it('accepts only years as years', async () => {
        expect((await resolve('/library/years/abc')).name).toBe('not-found')
    })

    it('assigns the main tabs', async () => {
        expect((await resolve('/now-playing')).meta.tab).toBe('now-playing')
        expect((await resolve('/playlists')).meta.tab).toBe('playlists')
    })

    it('shows the profile selection without the shell and without a profile', async () => {
        const route = await resolve('/profiles')

        expect(route.name).toBe('profiles')
        expect(route.meta.bare).toBe(true)
        expect(route.meta.public).toBe(true)
    })

    it('knows the equalizer below Now Playing', async () => {
        const route = await resolve('/now-playing/equalizer')

        expect(route.name).toBe('equalizer')
        expect(route.meta.tab).toBe('now-playing')
        expect(route.meta.public).toBeUndefined()
    })

    it('knows the settings page', async () => {
        const route = await resolve('/settings')

        expect(route.name).toBe('settings')
        expect(route.meta.bare).toBeUndefined()
    })

    it('opens the library of the administration for admins', async () => {
        const route = await resolve('/admin')

        expect(route.name).toBe('admin-library')
        expect(route.fullPath).toBe('/admin/library')
        expect(route.meta.admin).toBe(true)
    })

    it('keeps every page of the administration for admins', async () => {
        for (const [path, name] of [
            ['/admin/library', 'admin-library'],
            ['/admin/jobs', 'admin-jobs'],
            ['/admin/cache', 'admin-cache'],
            ['/admin/transcoding', 'admin-transcoding'],
            ['/admin/users', 'admin-users'],
        ]) {
            const route = await resolve(path!)
            expect(route.name, path).toBe(name)
            expect(route.meta.admin, path).toBe(true)
        }
    })

    it('protects the pages of the app', async () => {
        for (const path of [
            '/now-playing',
            '/library/songs',
            '/playlists',
            '/settings',
            '/unknown',
        ]) {
            expect((await resolve(path)).meta.public, path).toBeUndefined()
        }
    })

    it('shows unknown pages and sections as not found', async () => {
        expect((await resolve('/library/unknown')).name).toBe('not-found')
        expect((await resolve('/some/client/route')).name).toBe('not-found')
    })

    it('has no internal pages without the dev routes', async () => {
        const route = await resolve('/dev/components')

        expect(route.name).toBe('not-found')
        expect(createRoutes().some((record) => record.path.startsWith('/dev'))).toBe(false)
    })

    it('adds the internal pages of the development build before the catch-all route', async () => {
        const route = await resolve('/dev/components', createRoutes(devRoutes))

        expect(route.name).toBe('dev-components')
        expect(route.meta.bare).toBe(true)
        expect(route.meta.public).toBe(true)
        expect(createRoutes(devRoutes).at(-1)?.name).toBe('not-found')
    })
})
