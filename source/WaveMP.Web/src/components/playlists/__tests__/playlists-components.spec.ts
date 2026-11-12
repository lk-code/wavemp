import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { Playlist, Track } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'

const api = vi.hoisted(() => ({
    listPlaylists: vi.fn(),
    getPlaylist: vi.fn(),
    createPlaylist: vi.fn(),
    renamePlaylist: vi.fn(),
    deletePlaylist: vi.fn(),
    listPlaylistEntries: vi.fn(),
    addPlaylistEntries: vi.fn(),
    movePlaylistEntry: vi.fn(),
    removePlaylistEntry: vi.fn(),
    fetchPlaylistCover: vi.fn(),
    uploadPlaylistCover: vi.fn(),
    deletePlaylistCover: vi.fn(),
    listRecentlyPlayed: vi.fn(),
}))
const library = vi.hoisted(() => ({
    fetchAlbumCover: vi.fn(),
    fetchTrackCover: vi.fn(),
}))

vi.mock('@/playlists/api', () => api)
vi.mock('@/library/api', () => library)
vi.mock('@/player/api', () => ({ getPlayback: vi.fn(), recordPlay: vi.fn() }))

const AddToPlaylistDialog = (await import('../AddToPlaylistDialog.vue')).default
const PlaylistCover = (await import('../PlaylistCover.vue')).default
const PlaylistEditDialog = (await import('../PlaylistEditDialog.vue')).default
const PlaylistTrackList = (await import('../PlaylistTrackList.vue')).default

const roadTrip: Playlist = {
    id: 'road',
    name: 'Road Trip',
    trackCount: 2,
    cover: { kind: 'None', trackIds: [] },
}
const quiet: Playlist = {
    id: 'quiet',
    name: 'Quiet',
    trackCount: 0,
    cover: { kind: 'None', trackIds: [] },
}

const tracks: Track[] = [
    {
        id: 'opening',
        title: 'Opening',
        artist: { id: 'alpha', name: 'Alpha Band' },
        album: { id: 'first', title: 'First Album' },
        durationMilliseconds: 185_000,
        hasAudio: true,
        hasVideo: false,
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
    return wrapper
}

function problem(code: string, status = 409) {
    return Object.assign(new Error(code), { code, responseStatusCode: status })
}

beforeEach(() => {
    setActivePinia(createPinia())
    for (const mock of [...Object.values(api), ...Object.values(library)]) {
        mock.mockReset()
    }
    library.fetchTrackCover.mockResolvedValue(undefined)
    api.fetchPlaylistCover.mockResolvedValue(undefined)
})

afterEach(() => {
    vi.useRealTimers()
    document.body.innerHTML = ''
})

describe('PlaylistCover', () => {
    it('shows the placeholder icon without any cover', async () => {
        const wrapper = await mountWith(PlaylistCover, { id: 'road', cover: roadTrip.cover })

        expect(wrapper.find('[data-part="playlist-cover"]').exists()).toBe(false)
        expect(wrapper.find('[data-part="cover"] [data-icon="playlist"]').exists()).toBe(true)
        wrapper.unmount()
    })

    it('loads its own picture', async () => {
        const wrapper = await mountWith(PlaylistCover, {
            id: 'road',
            cover: { kind: 'Custom', version: 'v7', trackIds: [] },
        })

        expect(api.fetchPlaylistCover).toHaveBeenCalledWith('road', expect.any(Number))
        expect(library.fetchTrackCover).not.toHaveBeenCalled()
        wrapper.unmount()
    })

    it('shows the covers of its tracks one after the other', async () => {
        vi.useFakeTimers()
        const wrapper = await mountWith(PlaylistCover, {
            id: 'road',
            cover: { kind: 'Tracks', trackIds: ['t1', 't2', 't3'] },
        })

        const cover = wrapper.get('[data-part="playlist-cover"]')
        const active = () =>
            wrapper
                .findAll('[data-part="playlist-cover-slide"]')
                .map((slide) => slide.attributes('data-state') === 'active')
        expect(cover.attributes('data-count')).toBe('3')
        expect(cover.attributes('aria-hidden')).toBe('true')
        expect(active()).toEqual([true, false, false])

        await vi.advanceTimersByTimeAsync(6000)
        expect(active()).toEqual([false, true, false])
        await vi.advanceTimersByTimeAsync(12000)
        expect(active()).toEqual([true, false, false])
        wrapper.unmount()
    })
})

describe('AddToPlaylistDialog', () => {
    async function mountDialog() {
        const wrapper = await mountWith(AddToPlaylistDialog, { trackIds: ['opening'], open: false })
        await wrapper.setProps({ open: true })
        await flushPromises()
        return wrapper
    }

    function choices() {
        return [...document.querySelectorAll<HTMLButtonElement>('[data-part="playlist-choice"]')]
    }

    it('lists the playlists of the profile with their number of songs', async () => {
        api.listPlaylists.mockResolvedValue({ items: [roadTrip, quiet], total: 2 })
        const wrapper = await mountDialog()

        expect(
            choices().map((choice) => [
                choice.querySelector('[data-part="label"]')?.textContent,
                choice.querySelector('[data-part="playlist-choice-count"]')?.textContent?.trim(),
            ]),
        ).toEqual([
            ['Quiet', 'Songs: 0'],
            ['Road Trip', 'Songs: 2'],
        ])
        wrapper.unmount()
    })

    it('adds the tracks to the chosen playlist and closes', async () => {
        api.listPlaylists.mockResolvedValue({ items: [roadTrip], total: 1 })
        api.addPlaylistEntries.mockResolvedValue({
            entries: [],
            playlist: { ...roadTrip, trackCount: 3 },
        })
        const wrapper = await mountDialog()

        choices()[0]!.click()
        await flushPromises()

        expect(api.addPlaylistEntries).toHaveBeenCalledWith('road', ['opening'])
        expect(wrapper.emitted('added')?.[0]).toEqual([{ ...roadTrip, trackCount: 3 }])
        expect(wrapper.emitted('update:open')?.at(-1)).toEqual([false])
        wrapper.unmount()
    })

    it('explains why the tracks could not be added', async () => {
        api.listPlaylists.mockResolvedValue({ items: [roadTrip], total: 1 })
        api.addPlaylistEntries.mockRejectedValue(problem('playlist.full'))
        const wrapper = await mountDialog()

        choices()[0]!.click()
        await flushPromises()

        expect(document.body.textContent).toContain(
            'The playlist has reached its maximum number of songs.',
        )
        expect(wrapper.emitted('added')).toBeUndefined()
        wrapper.unmount()
    })

    it('says so when there are no playlists yet', async () => {
        api.listPlaylists.mockResolvedValue({ items: [], total: 0 })
        const wrapper = await mountDialog()

        expect(choices()).toEqual([])
        expect(document.body.textContent).toContain('You have no playlists yet.')
        wrapper.unmount()
    })
})

describe('PlaylistEditDialog', () => {
    async function mountDialog(props: Record<string, unknown>) {
        const wrapper = await mountWith(PlaylistEditDialog, { ...props, open: false })
        await wrapper.setProps({ open: true })
        await flushPromises()
        return wrapper
    }

    function nameInput() {
        return document.querySelector<HTMLInputElement>('[data-part="dialog"] input')!
    }

    async function submit(name: string) {
        const input = nameInput()
        input.value = name
        input.dispatchEvent(new Event('input'))
        document.querySelector('[data-part="dialog"] form')!.dispatchEvent(new Event('submit'))
        await flushPromises()
    }

    it('creates a playlist with the trimmed name and its first tracks', async () => {
        api.createPlaylist.mockResolvedValue(roadTrip)
        const wrapper = await mountDialog({ trackIds: ['opening'] })

        await submit('  Road Trip  ')

        expect(api.createPlaylist).toHaveBeenCalledWith('Road Trip', ['opening'])
        expect(wrapper.emitted('saved')?.[0]).toEqual([roadTrip])
        wrapper.unmount()
    })

    it('renames a playlist and asks for a name before saving', async () => {
        api.renamePlaylist.mockResolvedValue({ ...roadTrip, name: 'Long Drive' })
        const wrapper = await mountDialog({ playlist: roadTrip })
        expect(nameInput().value).toBe('Road Trip')

        await submit('   ')
        expect(api.renamePlaylist).not.toHaveBeenCalled()
        expect(document.body.textContent).toContain('Please enter a name.')

        await submit('Long Drive')
        expect(api.renamePlaylist).toHaveBeenCalledWith('road', 'Long Drive')
        wrapper.unmount()
    })
})

describe('PlaylistTrackList', () => {
    const rows = tracks.map((track, index) => ({ id: `entry-${index}`, track }))

    function columns(wrapper: Awaited<ReturnType<typeof mountWith>>) {
        return wrapper
            .findAll('[data-part="list-column"]')
            .map((column) => column.attributes('data-column'))
    }

    it('shows a handle in each row only when the entries can be moved', async () => {
        const sortable = await mountWith(PlaylistTrackList, {
            rows,
            label: 'Road Trip',
            sortable: true,
        })
        const fixed = await mountWith(PlaylistTrackList, {
            rows,
            label: 'Recently played',
            detailLabel: 'Last played',
        })

        expect(sortable.findAll('[data-part="drag-handle"]')).toHaveLength(2)
        expect(columns(sortable)).toEqual([
            'handle',
            'number',
            'title',
            'duration',
            'artist',
            'album',
            'rating',
            'favorite',
        ])
        expect(fixed.findAll('[data-part="drag-handle"]')).toHaveLength(0)
        expect(columns(fixed)).toEqual([
            'number',
            'title',
            'duration',
            'artist',
            'album',
            'detail',
            'rating',
            'favorite',
        ])
        sortable.unmount()
        fixed.unmount()
    })

    it('shows the entries in their order with album and duplicates', async () => {
        const wrapper = await mountWith(PlaylistTrackList, {
            rows: [...rows, { id: 'entry-2', track: tracks[0]! }],
            label: 'Road Trip',
        })

        const titles = wrapper
            .findAll('[data-part="list-row"] [data-column="title"] > [data-part="label"]')
            .map((cell) => cell.text())
        expect(titles).toEqual(['Opening', 'Clip', 'Opening'])
        expect(wrapper.find('[data-part="list-row"] [data-column="album"]').text()).toBe(
            'First Album',
        )
        wrapper.unmount()
    })
})
