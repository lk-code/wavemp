import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { Home } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'

const api = vi.hoisted(() => ({
    listFavorites: vi.fn(),
    addFavorite: vi.fn(),
    removeFavorite: vi.fn(),
    setTrackRating: vi.fn(),
    getHome: vi.fn(),
}))

vi.mock('@/collection/api', () => api)
vi.mock('@/library/api', () => ({ fetchAlbumCover: vi.fn(), fetchTrackCover: vi.fn() }))
vi.mock('@/playlists/api', () => ({ fetchPlaylistCover: vi.fn() }))

const FavoriteButton = (await import('../FavoriteButton.vue')).default
const RatingStars = (await import('../RatingStars.vue')).default
const LibraryScopeSwitch = (await import('../LibraryScopeSwitch.vue')).default
const FavoritesCollection = (await import('../FavoritesCollection.vue')).default
const HomeView = (await import('@/views/HomeView.vue')).default

async function mountWith(component: unknown, props: Record<string, unknown>, path = '/') {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push(path)
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const wrapper = mount(component as any, {
        props,
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, router }
}

describe('collection components', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('FavoriteButton toggles the favorite of its item', async () => {
        api.addFavorite.mockResolvedValue(undefined)
        api.removeFavorite.mockResolvedValue(undefined)
        const { wrapper } = await mountWith(FavoriteButton, { type: 'Album', id: 'a1' })
        const button = wrapper.get('[data-part="btn-favorite"]')

        expect(button.attributes('aria-pressed')).toBe('false')
        expect(button.attributes('data-state')).toBe('off')
        expect(button.get('[data-part="icon"]').attributes('data-icon')).toBe('favorite-off')
        expect(button.get('[data-part="label"]').text()).toBe('Favorite')

        await button.trigger('click')
        await flushPromises()
        expect(api.addFavorite).toHaveBeenCalledWith('Album', 'a1')
        expect(button.attributes('aria-pressed')).toBe('true')
        expect(button.get('[data-part="icon"]').attributes('data-icon')).toBe('favorite')

        await button.trigger('click')
        await flushPromises()
        expect(api.removeFavorite).toHaveBeenCalledWith('Album', 'a1')
        expect(button.attributes('data-state')).toBe('off')
        wrapper.unmount()
    })

    it('FavoriteButton is no tab stop in lists', async () => {
        const { wrapper } = await mountWith(FavoriteButton, {
            type: 'Track',
            id: 't1',
            favorite: true,
            focusable: false,
        })

        const button = wrapper.get('[data-part="btn-favorite"]')
        expect(button.attributes('tabindex')).toBe('-1')
        expect(button.attributes('aria-pressed')).toBe('true')
        wrapper.unmount()
    })

    it('RatingStars rates with a star and removes the rating with the same star', async () => {
        api.setTrackRating.mockResolvedValue(undefined)
        const { wrapper } = await mountWith(RatingStars, { trackId: 't1', rating: 2 })
        const rating = wrapper.get('[data-part="rating"]')
        const stars = () => wrapper.findAll('[data-part="rating-star"]')

        expect(rating.attributes('data-value')).toBe('2')
        expect(rating.attributes('data-state')).toBe('rated')
        expect(stars().map((star) => star.attributes('data-state'))).toEqual([
            'on',
            'on',
            'off',
            'off',
            'off',
        ])
        expect(stars()[1]!.attributes('aria-pressed')).toBe('true')
        expect(stars()[3]!.get('[data-part="label"]').text()).toBe('4 of 5 stars')

        await stars()[3]!.trigger('click')
        await flushPromises()
        expect(api.setTrackRating).toHaveBeenLastCalledWith('t1', 4)
        expect(rating.attributes('data-value')).toBe('4')

        await stars()[3]!.trigger('click')
        await flushPromises()
        expect(api.setTrackRating).toHaveBeenLastCalledWith('t1', 0)
        expect(rating.attributes('data-state')).toBe('unrated')
        wrapper.unmount()
    })

    it('LibraryScopeSwitch puts "my music" into the query and keeps the rest', async () => {
        const { wrapper, router } = await mountWith(
            LibraryScopeSwitch,
            {},
            '/library/songs?q=rock&sort=Title',
        )
        const all = wrapper.get('[data-item="all-music"]')
        const mine = wrapper.get('[data-item="my-music"]')
        expect(all.attributes('aria-pressed')).toBe('true')

        await mine.trigger('click')
        await flushPromises()
        expect(router.currentRoute.value.query).toEqual({
            q: 'rock',
            sort: 'Title',
            myMusic: 'true',
        })
        expect(mine.attributes('aria-pressed')).toBe('true')

        await all.trigger('click')
        await flushPromises()
        expect(router.currentRoute.value.query).toEqual({ q: 'rock', sort: 'Title' })
        wrapper.unmount()
    })

    it('FavoritesCollection shows songs, albums and artists of the profile', async () => {
        api.listFavorites.mockImplementation(({ type }: { type: string }) => {
            const items = {
                Track: [
                    {
                        type: 'Track',
                        itemId: 't1',
                        addedAt: new Date('2026-11-12T10:00:00Z'),
                        track: { id: 't1', title: 'Song', hasAudio: true, isFavorite: true },
                    },
                ],
                Album: [{ type: 'Album', itemId: 'a1', album: { id: 'a1', title: 'Album' } }],
                Artist: [],
            }[type]
            return Promise.resolve({ items, total: items?.length ?? 0 })
        })
        const { wrapper } = await mountWith(
            FavoritesCollection,
            { label: 'Favorites' },
            '/library/favorites',
        )

        expect(api.listFavorites).toHaveBeenCalledWith({ offset: 0, limit: 100, type: 'Track' })
        expect(
            wrapper
                .findAll('[data-part="section"]')
                .map((section) => section.attributes('data-item')),
        ).toEqual(['tracks', 'albums'])
        expect(
            wrapper.get('[data-part="list-cell"][data-column="title"] [data-part="label"]').text(),
        ).toBe('Song')
        expect(
            wrapper
                .get('[data-part="list-row"] [data-part="btn-favorite"]')
                .attributes('aria-pressed'),
        ).toBe('true')
        expect(wrapper.get('[data-part="tile-title"]').text()).toBe('Album')
        wrapper.unmount()
    })

    it('FavoritesCollection explains how to mark favorites without any', async () => {
        api.listFavorites.mockResolvedValue({ items: [], total: 0 })
        const { wrapper } = await mountWith(
            FavoritesCollection,
            { label: 'Favorites' },
            '/library/favorites',
        )

        expect(wrapper.get('[data-part="empty-state-title"]').text()).toBe('No favorites yet')
        wrapper.unmount()
    })
})

describe('HomeView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    afterEach(() => {
        document.body.innerHTML = ''
    })

    const page = (items: unknown[]) => ({ items, offset: 0, limit: 20, total: items.length })

    it('shows the rows with items in their order and links to the whole lists', async () => {
        const home: Home = {
            playlists: page([{ id: 'p1', name: 'Mix', trackCount: 2 }]),
            recentlyPlayed: page([{ track: { id: 't1', title: 'Song', hasAudio: true } }]),
            recentlyAdded: page([]),
            favorites: page([{ type: 'Artist', itemId: 'r1', artist: { id: 'r1', name: 'Band' } }]),
            topRated: page([{ id: 't2', title: 'Best', rating: 5, hasAudio: true }]),
        } as Home
        api.getHome.mockResolvedValue(home)

        const { wrapper } = await mountWith(HomeView, {})

        expect(api.getHome).toHaveBeenCalledWith(20)
        expect(
            wrapper.findAll('[data-part="shelf"]').map((shelf) => shelf.attributes('data-item')),
        ).toEqual(['playlists', 'recently-played', 'favorites', 'top-rated'])
        const topRated = wrapper.get('[data-part="shelf"][data-item="top-rated"]')
        expect(topRated.get('[data-part="shelf-title"]').text()).toBe('Top rated')
        expect(topRated.get('[data-part="tile"]').attributes('data-kind')).toBe('track')
        expect(topRated.get('[data-part="shelf-actions"] a').attributes('href')).toBe(
            '/library/songs?sort=Rating&myMusic=true',
        )
        expect(
            wrapper.get('[data-item="favorites"] [data-part="tile"]').attributes('data-kind'),
        ).toBe('artist')
        wrapper.unmount()
    })

    it('welcomes a profile without anything to show', async () => {
        api.getHome.mockResolvedValue({
            playlists: page([]),
            recentlyPlayed: page([]),
            recentlyAdded: page([]),
            favorites: page([]),
            topRated: page([]),
        })

        const { wrapper } = await mountWith(HomeView, {})

        expect(wrapper.find('[data-part="shelf"]').exists()).toBe(false)
        expect(wrapper.get('[data-part="empty-state-title"]').text()).toBe('Welcome to WaveMP')
        wrapper.unmount()
    })

    it('offers to try again when the home page cannot be loaded', async () => {
        api.getHome.mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce({})

        const { wrapper } = await mountWith(HomeView, {})
        expect(wrapper.get('[data-part="empty-state-title"]').text()).toBe(
            'The home page could not be loaded.',
        )

        await wrapper.get('[data-part="empty-state"] button').trigger('click')
        await flushPromises()
        expect(api.getHome).toHaveBeenCalledTimes(2)
        wrapper.unmount()
    })
})
