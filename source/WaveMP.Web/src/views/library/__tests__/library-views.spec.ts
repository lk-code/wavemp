import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { Track } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'
import { usePageTitleStore } from '@/stores/pageTitle'
import { usePlayerStore } from '@/stores/player'
import { useSessionStore } from '@/stores/session'

const api = vi.hoisted(() => ({
    listTracks: vi.fn(),
    listAlbums: vi.fn(),
    listArtists: vi.fn(),
    listGenres: vi.fn(),
    listYears: vi.fn(),
    getAlbum: vi.fn(),
    listAlbumTracks: vi.fn(),
    getArtist: vi.fn(),
    listArtistAlbums: vi.fn(),
    getTrack: vi.fn(),
    listTrackFiles: vi.fn(),
    getTrackMetadata: vi.fn(),
    updateTrackMetadata: vi.fn(),
    searchLibrary: vi.fn(),
    fetchAlbumCover: vi.fn(),
    fetchTrackCover: vi.fn(),
}))

vi.mock('@/library/api', () => api)

const LibraryView = (await import('../LibraryView.vue')).default
const AlbumView = (await import('../AlbumView.vue')).default
const SearchView = (await import('../SearchView.vue')).default
const TrackView = (await import('../TrackView.vue')).default

const alpha = { id: 'alpha', name: 'Alpha Band' }
const album = { id: 'first', title: 'First Album', artist: alpha, isCompilation: false }

function track(id: string, values: Partial<Track> = {}): Track {
    return { id, title: id, artist: alpha, album, hasAudio: true, hasVideo: false, ...values }
}

async function mountView(component: unknown, path: string, props: Record<string, unknown> = {}) {
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

describe('LibraryView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('shows the songs grouped by album with the sort order of the route', async () => {
        api.listTracks.mockResolvedValue({
            items: [track('Opening', { trackNumber: 1 }), track('Closing', { trackNumber: 2 })],
            total: 2,
        })

        const { wrapper } = await mountView(
            LibraryView,
            '/library/songs?sort=Title&direction=Desc',
            { section: 'songs' },
        )

        expect(api.listTracks).toHaveBeenCalledWith(
            expect.objectContaining({ offset: 0, limit: 100, sort: 'Title', direction: 'Desc' }),
        )
        expect(wrapper.get('[data-part="list-group-title"]').text()).toBe('First Album')
        expect(wrapper.findAll('[data-part="list-row"]')).toHaveLength(2)
        expect(wrapper.find('[data-part="placeholder-notice"]').exists()).toBe(false)
        wrapper.unmount()
    })

    it('shows the recently added songs newest first', async () => {
        api.listTracks.mockResolvedValue({ items: [track('New')], total: 1 })

        const { wrapper } = await mountView(LibraryView, '/library/recently-added', {
            section: 'recently-added',
        })

        expect(api.listTracks).toHaveBeenCalledWith(
            expect.objectContaining({ sort: 'FirstSeenAt', direction: 'Desc' }),
        )
        wrapper.unmount()
    })

    it('shows albums, artists, genres and years as tiles', async () => {
        api.listAlbums.mockResolvedValue({
            items: [{ id: 'first', title: 'First Album', artist: alpha, hasCover: false }],
            total: 1,
        })
        api.listGenres.mockResolvedValue({ items: [{ name: 'Rock', trackCount: 2 }], total: 1 })

        const albums = await mountView(LibraryView, '/library/albums', { section: 'albums' })
        const genres = await mountView(LibraryView, '/library/genres', { section: 'genres' })

        const albumTile = albums.wrapper.get('[data-part="tile"]')
        expect(albumTile.attributes('href')).toBe('/library/albums/first')
        expect(albumTile.get('[data-part="tile-subtitle"]').text()).toBe('Alpha Band')
        const genreTile = genres.wrapper.get('[data-part="tile"]')
        expect(genreTile.attributes('href')).toBe('/library/genres/Rock')
        expect(genreTile.get('[data-part="tile-subtitle"]').text()).toBe('Songs: 2')
        albums.wrapper.unmount()
        genres.wrapper.unmount()
    })

    it('tells admins how to fill an empty library', async () => {
        api.listTracks.mockResolvedValue({ items: [], total: 0 })
        const session = useSessionStore()
        session.$patch({
            token: 'token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            profile: {
                id: 'admin',
                name: 'Admin',
                initials: 'A',
                color: '#1f6fd6',
                role: 'Admin',
                isBuiltIn: true,
            },
        })

        const { wrapper } = await mountView(LibraryView, '/library/songs', { section: 'songs' })

        expect(wrapper.get('[data-part="empty-state-title"]').text()).toBe('Your library is empty')
        expect(wrapper.get('[data-part="empty-state"] [data-part="link"]').attributes('href')).toBe(
            '/admin/library',
        )
        wrapper.unmount()
    })

    it('shows the search text of a filtered list and offers all entries', async () => {
        api.listArtists.mockResolvedValue({ items: [], total: 0 })

        const { wrapper } = await mountView(LibraryView, '/library/artists?q=gust', {
            section: 'artists',
        })

        expect(api.listArtists).toHaveBeenCalledWith(expect.objectContaining({ q: 'gust' }))
        expect(wrapper.get('[data-part="alert"]').text()).toContain('“gust”')
        expect(wrapper.get('[data-part="view"] > [data-part="link"]').attributes('href')).toBe(
            '/library/artists',
        )
        expect(wrapper.get('[data-part="empty-state-text"]').text()).toBe(
            'Nothing in this list matches “gust”.',
        )
        wrapper.unmount()
    })

    it('ignores a search text in the years, which cannot be filtered', async () => {
        api.listYears.mockResolvedValue({
            items: [{ year: 1999, trackCount: 3, albumCount: 1 }],
            total: 1,
        })

        const { wrapper } = await mountView(LibraryView, '/library/years?q=gust', {
            section: 'years',
        })

        expect(api.listYears).toHaveBeenCalledWith(expect.not.objectContaining({ q: 'gust' }))
        expect(wrapper.find('[data-part="alert"]').exists()).toBe(false)
        expect(wrapper.find('[data-part="view"] > [data-part="link"]').exists()).toBe(false)
        expect(wrapper.findAll('[data-part="tile"]')).toHaveLength(1)
        wrapper.unmount()
    })

    it('offers to try again after an error', async () => {
        api.listTracks.mockRejectedValueOnce(new Error('offline'))
        api.listTracks.mockResolvedValueOnce({ items: [track('Back')], total: 1 })

        const { wrapper } = await mountView(LibraryView, '/library/songs', { section: 'songs' })
        expect(wrapper.get('[data-part="empty-state-title"]').text()).toBe(
            'The library could not be loaded.',
        )

        await wrapper.get('[data-part="empty-state"] [data-part="button"]').trigger('click')
        await flushPromises()

        expect(wrapper.findAll('[data-part="list-row"]')).toHaveLength(1)
        wrapper.unmount()
    })
})

describe('AlbumView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('shows the album with its details and its tracks by disc', async () => {
        api.getAlbum.mockResolvedValue({
            id: 'first',
            title: 'First Album',
            artist: alpha,
            year: 2001,
            genre: 'Rock',
            trackCount: 2,
            durationMilliseconds: 125_000,
            hasCover: false,
        })
        api.listAlbumTracks.mockResolvedValue({
            items: [track('One', { discNumber: 1 }), track('Two', { discNumber: 2 })],
            total: 2,
        })

        const { wrapper } = await mountView(AlbumView, '/library/albums/first', { id: 'first' })

        expect(wrapper.get('[data-part="view"]').attributes('data-view')).toBe('library-album')
        expect(wrapper.get('[data-part="detail-title"]').text()).toBe('First Album')
        expect(wrapper.get('[data-field="artist"] [data-part="link"]').attributes('href')).toBe(
            '/library/artists/alpha',
        )
        expect(wrapper.get('[data-field="duration"] [data-part="meta-value"]').text()).toBe('2:05')
        expect(
            wrapper.findAll('[data-part="list-group-title"]').map((title) => title.text()),
        ).toEqual(['Disc 1', 'Disc 2'])
        expect(wrapper.find('[data-part="list-group-info"]').exists()).toBe(false)
        expect(usePageTitleStore().title).toBe('First Album')

        wrapper.unmount()
        expect(usePageTitleStore().title).toBeNull()
    })

    it('plays the album in order or shuffled', async () => {
        api.getAlbum.mockResolvedValue({ id: 'first', title: 'First Album', artist: alpha })
        api.listAlbumTracks.mockResolvedValue({
            items: [track('One'), track('Clip', { hasAudio: false }), track('Two')],
            total: 3,
        })
        const player = usePlayerStore()
        const playTracks = vi.spyOn(player, 'playTracks').mockImplementation(() => undefined)

        const { wrapper } = await mountView(AlbumView, '/library/albums/first', { id: 'first' })
        await wrapper.get('[data-part="detail-actions"] [data-item="play"]').trigger('click')
        await wrapper.get('[data-part="detail-actions"] [data-item="shuffle"]').trigger('click')

        const ids = (call: unknown[]) => (call[0] as Track[]).map((item) => item.id)
        expect(ids(playTracks.mock.calls[0]!)).toEqual(['One', 'Two'])
        expect(playTracks.mock.calls[0]!.slice(1)).toEqual([0, { shuffle: false }])
        expect(playTracks.mock.calls[1]!.slice(1)).toEqual([-1, { shuffle: true }])
        wrapper.unmount()
    })

    it('tells when the album no longer exists', async () => {
        api.getAlbum.mockRejectedValue({ responseStatusCode: 404, code: 'album.not_found' })
        api.listAlbumTracks.mockRejectedValue({ responseStatusCode: 404 })

        const { wrapper } = await mountView(AlbumView, '/library/albums/gone', { id: 'gone' })

        expect(wrapper.get('[data-part="empty-state-text"]').text()).toBe(
            'This album no longer exists.',
        )
        wrapper.unmount()
    })
})

describe('SearchView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('asks for a text without search text', async () => {
        const { wrapper } = await mountView(SearchView, '/library/search')

        expect(wrapper.get('[data-part="empty-state-title"]').text()).toBe('Search your library')
        expect(api.searchLibrary).not.toHaveBeenCalled()
        wrapper.unmount()
    })

    it('shows the artists, albums and songs found with links to the filtered lists', async () => {
        api.searchLibrary.mockResolvedValue({
            artists: { items: [{ id: 'alpha', name: 'Alpha Band', trackCount: 2 }], total: 1 },
            albums: { items: [], total: 0 },
            tracks: { items: [track('Ballad')], total: 1 },
        })

        const { wrapper } = await mountView(SearchView, '/library/search?q=alpa')

        expect(api.searchLibrary).toHaveBeenCalledWith('alpa', 10)
        const sections = wrapper.findAll('[data-part="section"]')
        expect(sections.map((section) => section.attributes('data-item'))).toEqual([
            'artists',
            'tracks',
        ])
        expect(sections[0]!.get('[data-part="tile-title"]').text()).toBe('Alpha Band')
        expect(sections[0]!.get('section > [data-part="link"]').attributes('href')).toBe(
            '/library/artists?q=alpa',
        )
        expect(sections[1]!.get('[data-part="list-row"]').text()).toContain('Ballad')
        wrapper.unmount()
    })

    it('tells when nothing matches', async () => {
        api.searchLibrary.mockResolvedValue({
            artists: { items: [], total: 0 },
            albums: { items: [], total: 0 },
            tracks: { items: [], total: 0 },
        })

        const { wrapper } = await mountView(SearchView, '/library/search?q=xyz')

        expect(wrapper.get('[data-part="empty-state-text"]').text()).toBe('Nothing matches “xyz”.')
        wrapper.unmount()
    })
})

describe('TrackView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
        api.getTrack.mockResolvedValue(
            track('Closing', {
                trackNumber: 2,
                year: 2001,
                hasVideo: true,
                durationMilliseconds: 61_000,
            }),
        )
        api.listTrackFiles.mockResolvedValue({
            items: [
                { id: 'f', kind: 'Audio', relativePath: 'Band/Album/02 - Closing.flac', size: 10 },
            ],
            total: 1,
        })
    })

    function signIn(role: 'Admin' | 'User') {
        useSessionStore().$patch({
            token: 'token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            profile: {
                id: 'p',
                name: 'Profile',
                initials: 'P',
                color: '#1f6fd6',
                role,
                isBuiltIn: false,
            },
        })
    }

    it('shows the details of the song and its files to admins', async () => {
        signIn('Admin')

        const { wrapper } = await mountView(TrackView, '/library/tracks/Closing', { id: 'Closing' })

        expect(wrapper.get('[data-part="detail-title"]').text()).toBe('Closing')
        expect(wrapper.get('[data-field="media"] [data-part="meta-value"]').text()).toBe(
            'Audio and video',
        )
        expect(wrapper.get('[data-field="album"] [data-part="link"]').attributes('href')).toBe(
            '/library/albums/first',
        )
        expect(api.listTrackFiles).toHaveBeenCalledWith('Closing', { offset: 0, limit: 100 })
        expect(wrapper.get('[data-part="section"][data-item="files"]').text()).toContain(
            '02 - Closing.flac',
        )
        wrapper.unmount()
    })

    it('hides the files from other profiles', async () => {
        signIn('User')

        const { wrapper } = await mountView(TrackView, '/library/tracks/Closing', { id: 'Closing' })

        expect(wrapper.find('[data-item="files"]').exists()).toBe(false)
        expect(api.listTrackFiles).not.toHaveBeenCalled()
        wrapper.unmount()
    })

    describe('metadata editor', () => {
        const metadata = {
            trackId: 'Closing',
            status: 'Editable',
            audioFilePath: 'Band/Album/02 - Closing.flac',
            title: 'Closing',
            artist: 'Alpha Band',
        }

        beforeEach(() => {
            api.getTrackMetadata.mockResolvedValue(metadata)
            api.updateTrackMetadata.mockResolvedValue({ ...metadata, title: 'Renamed' })
        })

        it('is offered to admins only', async () => {
            signIn('User')
            const user = await mountView(TrackView, '/library/tracks/Closing', { id: 'Closing' })

            expect(user.wrapper.find('[data-item="edit-metadata"]').exists()).toBe(false)
            expect(
                user.wrapper.find('[data-part="detail-actions"] [data-item="play"]').exists(),
            ).toBe(true)
            expect(user.wrapper.find('[data-part="dialog"]').exists()).toBe(false)
            user.wrapper.unmount()

            signIn('Admin')
            const admin = await mountView(TrackView, '/library/tracks/Closing', { id: 'Closing' })

            const button = admin.wrapper.get(
                '[data-part="detail-actions"] [data-item="edit-metadata"]',
            )
            expect(button.text()).toBe('Edit metadata')
            expect(api.getTrackMetadata).not.toHaveBeenCalled()
            admin.wrapper.unmount()
        })

        it('opens with the button and loads the song again after saving', async () => {
            signIn('Admin')
            const { wrapper } = await mountView(TrackView, '/library/tracks/Closing', {
                id: 'Closing',
            })

            await wrapper.get('[data-item="edit-metadata"]').trigger('click')
            await flushPromises()
            expect(api.getTrackMetadata).toHaveBeenCalledWith('Closing')

            await wrapper.get('[data-part="field"][data-field="title"] input').setValue('Renamed')
            await wrapper.get('[data-part="dialog"] form').trigger('submit')
            await flushPromises()

            expect(api.updateTrackMetadata).toHaveBeenCalledWith('Closing', { title: 'Renamed' })
            expect(api.getTrack).toHaveBeenCalledTimes(2)
            expect(api.listTrackFiles).toHaveBeenCalledTimes(2)
            wrapper.unmount()
        })

        it('opens from the address and removes the request when it closes', async () => {
            signIn('Admin')
            const { wrapper, router } = await mountView(
                TrackView,
                '/library/tracks/Closing?edit=metadata',
                { id: 'Closing' },
            )

            expect(api.getTrackMetadata).toHaveBeenCalledWith('Closing')
            await wrapper.get('[data-part="dialog-footer"] button[type="button"]').trigger('click')
            await flushPromises()

            expect(router.currentRoute.value.query.edit).toBeUndefined()
            expect(router.currentRoute.value.path).toBe('/library/tracks/Closing')
            expect(api.getTrackMetadata).toHaveBeenCalledTimes(1)
            wrapper.unmount()
        })

        it('ignores the request from the address for other profiles', async () => {
            signIn('User')
            const { wrapper } = await mountView(
                TrackView,
                '/library/tracks/Closing?edit=metadata',
                { id: 'Closing' },
            )

            expect(api.getTrackMetadata).not.toHaveBeenCalled()
            wrapper.unmount()
        })
    })
})
