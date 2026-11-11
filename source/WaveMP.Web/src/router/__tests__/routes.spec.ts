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

    it('opens the songs of the library on the start page', async () => {
        const route = await resolve('/')

        expect(route.name).toBe('library-section')
        expect(route.fullPath).toBe('/library/songs')
        expect(route.meta.tab).toBe('library')
    })

    it('redirects the library to its songs', async () => {
        expect((await resolve('/library')).fullPath).toBe('/library/songs')
    })

    it('knows the sections of the library', async () => {
        for (const section of ['recently-added', 'artists', 'albums', 'songs', 'genres', 'years']) {
            const route = await resolve(`/library/${section}`)
            expect(route.name).toBe('library-section')
            expect(route.params.section).toBe(section)
        }
    })

    it('assigns the main tabs', async () => {
        expect((await resolve('/now-playing')).meta.tab).toBe('now-playing')
        expect((await resolve('/playlists')).meta.tab).toBe('playlists')
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
        expect(createRoutes(devRoutes).at(-1)?.name).toBe('not-found')
    })
})
