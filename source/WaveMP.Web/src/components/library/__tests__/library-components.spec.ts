import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { Track } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { groupTracksByAlbum } from '@/library/grouping'
import { createRoutes } from '@/router/routes'

const covers = vi.hoisted(() => ({
    fetchAlbumCover: vi.fn(),
    fetchTrackCover: vi.fn(),
}))

vi.mock('@/library/api', () => covers)

const LibraryCover = (await import('../LibraryCover.vue')).default
const TileList = (await import('../TileList.vue')).default
const TrackFileList = (await import('../TrackFileList.vue')).default
const TrackGroupList = (await import('../TrackGroupList.vue')).default

const alpha = { id: 'alpha', name: 'Alpha Band' }
const album = { id: 'first', title: 'First Album', artist: alpha, isCompilation: false }
const tracks: Track[] = [
    {
        id: 'opening',
        title: 'Opening',
        artist: alpha,
        album,
        trackNumber: 1,
        year: 2001,
        genre: 'Rock',
        durationMilliseconds: 185_000,
        hasAudio: true,
        hasVideo: false,
        hasCover: false,
    },
    {
        id: 'closing',
        title: 'Closing',
        artist: alpha,
        album,
        trackNumber: 2,
        durationMilliseconds: 61_000,
        hasAudio: true,
        hasVideo: true,
        hasCover: false,
    },
    { id: 'clip', title: 'Clip', hasAudio: false, hasVideo: true, hasCover: false },
]

async function mountWith(component: unknown, props: Record<string, unknown>) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push('/library/songs')
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const wrapper = mount(component as any, {
        props,
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, router }
}

describe('TrackGroupList', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows the tracks grouped by album with links to album, artist, genre and year', async () => {
        const { wrapper } = await mountWith(TrackGroupList, {
            groups: groupTracksByAlbum(tracks),
            label: 'Songs',
        })

        const groups = wrapper.findAll('[data-part="list-group"]')
        expect(groups.map((group) => group.get('[data-part="list-group-title"]').text())).toEqual([
            'First Album',
            'Unknown album',
        ])
        const links = groups[0]!
            .findAll('[data-part="meta-value"] > [data-part="link"]')
            .map((link) => link.attributes('href'))
        expect(links).toEqual([
            '/library/albums/first',
            '/library/artists/alpha',
            '/library/genres/Rock',
            '/library/years/2001',
        ])
        expect(groups[1]!.get('[data-field="artist"] [data-part="meta-value"]').text()).toBe(
            'Unknown artist',
        )
        wrapper.unmount()
    })

    it('shows number, title, length, artist and the video badges', async () => {
        const { wrapper } = await mountWith(TrackGroupList, {
            groups: groupTracksByAlbum(tracks),
            label: 'Songs',
        })

        const rows = wrapper.findAll('[data-part="list-row"]')
        expect(rows[0]!.findAll('[data-part="list-cell"]').map((cell) => cell.text())).toEqual([
            '1',
            'Opening',
            '3:05',
            'Alpha Band',
        ])
        expect(rows[1]!.attributes('data-state')).toBe('has-video')
        expect(rows[1]!.get('[data-part="badge"]').attributes('data-state')).toBe('has-video')
        expect(rows[2]!.attributes('data-state')).toBe('has-video video-only')
        expect(rows[2]!.get('[data-part="badge"]').attributes('data-state')).toBe(
            'has-video video-only',
        )
        expect(rows[2]!.get('[data-part="badge"] [data-part="label"]').text()).toBe('Video only')
        wrapper.unmount()
    })

    it('opens the menu of a track with a right click and leads to its details', async () => {
        const { wrapper, router } = await mountWith(TrackGroupList, {
            groups: groupTracksByAlbum(tracks),
            label: 'Songs',
        })

        await wrapper.findAll('[data-part="list-row"]')[0]!.trigger('contextmenu', {
            clientX: 10,
            clientY: 20,
        })
        await flushPromises()

        const menu = document.querySelector('[data-part="context-menu"]')!
        expect(
            [...menu.querySelectorAll('[data-part="menu-item"]')].map((item) =>
                item.getAttribute('data-item'),
            ),
        ).toEqual(['details', 'album', 'artist'])
        ;(menu.querySelector('[data-item="details"]') as HTMLElement).click()
        await flushPromises()
        expect(router.currentRoute.value.fullPath).toBe('/library/tracks/opening')
        wrapper.unmount()
    })

    it('opens the menu with the keyboard and leaves out missing album and artist', async () => {
        const { wrapper } = await mountWith(TrackGroupList, {
            groups: groupTracksByAlbum(tracks),
            label: 'Songs',
        })

        await wrapper.findAll('[data-part="list-row"]')[2]!.trigger('keydown', {
            key: 'F10',
            shiftKey: true,
        })
        await flushPromises()

        const items = [...document.querySelectorAll('[data-part="menu-item"]')].map((item) =>
            item.getAttribute('data-item'),
        )
        expect(items).toEqual(['details'])
        wrapper.unmount()
    })

    it('leaves out the details of the groups on request', async () => {
        const { wrapper } = await mountWith(TrackGroupList, {
            groups: groupTracksByAlbum(tracks),
            label: 'Songs',
            showGroupInfo: false,
        })

        expect(wrapper.find('[data-part="list-group-info"]').exists()).toBe(false)
        wrapper.unmount()
    })
})

describe('TileList', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('links every tile to its page and shows the icon of its kind without cover', async () => {
        const { wrapper } = await mountWith(TileList, {
            label: 'Artists',
            tiles: [
                {
                    id: 'alpha',
                    kind: 'artist',
                    title: 'Alpha Band',
                    subtitle: 'Songs: 3',
                    to: { name: 'library-artist', params: { id: 'alpha' } },
                },
            ],
        })

        const tile = wrapper.get('[data-part="tile"]')
        expect(tile.element.tagName).toBe('A')
        expect(tile.attributes('href')).toBe('/library/artists/alpha')
        expect(tile.attributes('data-kind')).toBe('artist')
        expect(tile.get('[data-part="tile-title"]').text()).toBe('Alpha Band')
        expect(tile.get('[data-part="tile-subtitle"]').text()).toBe('Songs: 3')
        expect(tile.get('[data-part="cover"] [data-part="icon"]').attributes('data-icon')).toBe(
            'artist',
        )
        wrapper.unmount()
    })
})

describe('LibraryCover', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        covers.fetchAlbumCover.mockReset().mockResolvedValue(new Uint8Array([1]).buffer)
        vi.stubGlobal('URL', {
            ...URL,
            createObjectURL: () => 'blob:album',
            revokeObjectURL: vi.fn(),
        })
    })

    afterEach(() => {
        vi.unstubAllGlobals()
    })

    it('shows the cover once it is loaded', async () => {
        const { wrapper } = await mountWith(LibraryCover, {
            kind: 'album',
            id: 'a',
            available: true,
        })

        expect(wrapper.get('[data-part="cover-image"]').attributes('src')).toBe('blob:album')
        expect(wrapper.get('[data-part="cover"]').attributes('data-state')).toBeUndefined()
        wrapper.unmount()
    })

    it('shows the placeholder and loads nothing without a cover', async () => {
        const { wrapper } = await mountWith(LibraryCover, {
            kind: 'album',
            id: 'a',
            available: false,
        })

        expect(wrapper.find('[data-part="cover-image"]').exists()).toBe(false)
        expect(wrapper.get('[data-part="cover"]').attributes('data-state')).toBe('placeholder')
        expect(covers.fetchAlbumCover).not.toHaveBeenCalled()
        wrapper.unmount()
    })
})

describe('TrackFileList', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows path, kind, use and technical details of every file', async () => {
        const { wrapper } = await mountWith(TrackFileList, {
            label: 'Files',
            files: [
                {
                    id: 'flac',
                    kind: 'Audio',
                    relativePath: 'Band/Album/02 - Song.flac',
                    isPrimary: true,
                    isLossless: true,
                    isWritable: true,
                    codec: 'flac',
                    bitrate: 912_400,
                    sampleRate: 44_100,
                    bitDepth: 16,
                    channels: 2,
                    durationMilliseconds: 61_000,
                    size: 4_200_000,
                },
                {
                    id: 'mkv',
                    kind: 'Video',
                    relativePath: 'Band/Album/02 - Song.mkv',
                    isPrimary: false,
                    width: 1920,
                    height: 1080,
                    size: 999,
                    issue: 'file.probe_failed',
                },
            ],
        })

        const items = wrapper.findAll('[data-part="file-item"]')
        expect(items.map((item) => item.attributes('data-state'))).toEqual([
            'audio primary',
            'video',
        ])
        expect(items[0]!.get('[data-part="file-path"]').text()).toBe('Band/Album/02 - Song.flac')
        const badges = items[0]!
            .findAll('[data-part="badge"]')
            .map((badge) => badge.attributes('data-state'))
        expect(badges).toEqual(['audio', 'primary', 'lossless', 'writable'])
        const details = items[0]!
            .findAll('[data-part="file-meta"] > [data-part="label"]')
            .map((label) => label.text())
        expect(details).toEqual([
            'FLAC',
            '912 kbit/s',
            '44.1 kHz',
            '16 bit',
            'Channels: 2',
            '1:01',
            '4.2 MB',
        ])
        expect(items[1]!.get('[data-part="badge"][data-state="issue"]').text()).toBe(
            'No media information',
        )
        expect(items[1]!.text()).toContain('1920 × 1080')
        wrapper.unmount()
    })
})
