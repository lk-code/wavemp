import { describe, expect, it } from 'vitest'
import { createMemoryHistory, createRouter } from 'vue-router'
import { resolveAccess, safeRedirect, type AccessState } from '../guards'
import { createRoutes, devRoutes } from '../routes'

const guest: AccessState = { isAuthenticated: false, isAdmin: false }
const user: AccessState = { isAuthenticated: true, isAdmin: false }
const admin: AccessState = { isAuthenticated: true, isAdmin: true }

async function navigate(path: string, state: AccessState) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes(devRoutes) })
    router.beforeEach((to) => resolveAccess(to, state))
    await router.push(path)
    return router.currentRoute.value
}

describe('resolveAccess', () => {
    it('opens the profile selection without a profile and leads back afterwards', async () => {
        const route = await navigate('/now-playing', guest)

        expect(route.name).toBe('profiles')
        expect(route.query).toEqual({ redirect: '/now-playing' })
    })

    it('opens the profile selection on the start page without a way back', async () => {
        const route = await navigate('/', guest)

        expect(route.fullPath).toBe('/profiles')
    })

    it('keeps the full path of deep links', async () => {
        const route = await navigate('/library/albums?sort=year', guest)

        expect(route.query.redirect).toBe('/library/albums?sort=year')
    })

    it('lets everyone open public pages', async () => {
        expect((await navigate('/profiles', guest)).name).toBe('profiles')
        expect((await navigate('/dev/components', guest)).name).toBe('dev-components')
    })

    it('lets profiles open the app', async () => {
        expect((await navigate('/now-playing', user)).name).toBe('now-playing')
        expect((await navigate('/', user)).fullPath).toBe('/library/songs')
    })

    it('keeps admin pages for admins', () => {
        const adminPage = {
            fullPath: '/admin/users',
            meta: { admin: true },
            redirectedFrom: undefined,
        }

        expect(resolveAccess(adminPage, user)).toEqual({ name: 'library' })
        expect(resolveAccess(adminPage, admin)).toBe(true)
        expect(resolveAccess(adminPage, guest)).toEqual({
            name: 'profiles',
            query: { redirect: '/admin/users' },
        })
    })
})

describe('safeRedirect', () => {
    it('keeps paths within the app', () => {
        expect(safeRedirect('/now-playing')).toBe('/now-playing')
        expect(safeRedirect('/library/albums?sort=year#top')).toBe('/library/albums?sort=year#top')
    })

    it('falls back to the start page for anything else', () => {
        for (const value of [
            undefined,
            null,
            42,
            ['/now-playing'],
            '',
            'now-playing',
            'https://example.com/',
            '//example.com/',
            '/\\example.com',
            '/profiles',
            '/profiles?redirect=/',
        ]) {
            expect(safeRedirect(value), String(value)).toBe('/')
        }
    })

    it('keeps paths that only start like the profile selection', () => {
        expect(safeRedirect('/profiles-old')).toBe('/profiles-old')
    })
})
