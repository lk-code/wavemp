import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import type { Playlist } from '@/api/generated/models'

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

vi.mock('@/playlists/api', () => api)

const { usePlaylistsStore } = await import('../playlists')
const { useSessionStore } = await import('../session')

function playlist(id: string, name: string, trackCount = 0): Playlist {
    return { id, name, trackCount }
}

describe('playlists store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('loads all pages of the playlists and sorts them by name', async () => {
        api.listPlaylists
            .mockResolvedValueOnce({
                items: [playlist('2', 'beta'), playlist('1', 'Alpha')],
                total: 3,
            })
            .mockResolvedValueOnce({ items: [playlist('3', 'Charlie')], total: 3 })
        const store = usePlaylistsStore()

        await store.load()

        expect(store.status).toBe('ready')
        expect(store.playlists.map((item) => item.name)).toEqual(['Alpha', 'beta', 'Charlie'])
        expect(api.listPlaylists).toHaveBeenNthCalledWith(1, {
            offset: 0,
            limit: 100,
            sort: 'Name',
        })
        expect(api.listPlaylists).toHaveBeenNthCalledWith(2, {
            offset: 2,
            limit: 100,
            sort: 'Name',
        })
    })

    it('reports a failed load and loads again when asked', async () => {
        api.listPlaylists.mockRejectedValueOnce(new Error('offline'))
        const store = usePlaylistsStore()

        await store.ensureLoaded()
        expect(store.status).toBe('error')

        api.listPlaylists.mockResolvedValueOnce({ items: [playlist('1', 'Alpha')], total: 1 })
        await store.ensureLoaded()
        await store.ensureLoaded()

        expect(store.status).toBe('ready')
        expect(api.listPlaylists).toHaveBeenCalledTimes(2)
    })

    it('takes over created, renamed and changed playlists', async () => {
        api.listPlaylists.mockResolvedValue({ items: [playlist('1', 'Alpha')], total: 1 })
        api.createPlaylist.mockResolvedValue(playlist('2', 'Bravo', 1))
        api.renamePlaylist.mockResolvedValue(playlist('1', 'Zulu'))
        api.addPlaylistEntries.mockResolvedValue({
            entries: [],
            playlist: playlist('2', 'Bravo', 3),
        })
        const store = usePlaylistsStore()
        await store.load()

        const created = await store.create('Bravo', ['t1'])
        await store.rename('1', 'Zulu')
        const changed = await store.addTracks('2', ['t1', 't1'])

        expect(created.id).toBe('2')
        expect(api.createPlaylist).toHaveBeenCalledWith('Bravo', ['t1'])
        expect(api.addPlaylistEntries).toHaveBeenCalledWith('2', ['t1', 't1'])
        expect(changed?.trackCount).toBe(3)
        expect(store.playlists.map((item) => [item.name, item.trackCount])).toEqual([
            ['Bravo', 3],
            ['Zulu', 0],
        ])
        expect(store.find('2')?.trackCount).toBe(3)
    })

    it('fails when the API returns no created playlist', async () => {
        api.createPlaylist.mockResolvedValue(undefined)

        await expect(usePlaylistsStore().create('Empty')).rejects.toThrow()
    })

    it('removes deleted playlists and keeps them when deleting fails', async () => {
        api.listPlaylists.mockResolvedValue({
            items: [playlist('1', 'Alpha'), playlist('2', 'Bravo')],
            total: 2,
        })
        api.deletePlaylist.mockResolvedValueOnce(undefined).mockRejectedValueOnce(new Error('no'))
        const store = usePlaylistsStore()
        await store.load()

        await store.remove('1')
        await expect(store.remove('2')).rejects.toThrow()

        expect(store.playlists.map((item) => item.id)).toEqual(['2'])
    })

    it('forgets the playlists when the profile changes', async () => {
        api.listPlaylists.mockResolvedValue({ items: [playlist('1', 'Alpha')], total: 1 })
        const store = usePlaylistsStore()
        const session = useSessionStore()
        await store.load()

        session.token = 'other-profile'
        await nextTick()

        expect(store.playlists).toEqual([])
        expect(store.status).toBe('idle')
    })

    it('ignores a load that was overtaken by a change of the profile', async () => {
        let resolve: (value: unknown) => void = () => {}
        api.listPlaylists.mockReturnValueOnce(new Promise((done) => (resolve = done)))
        const store = usePlaylistsStore()

        const loading = store.load()
        store.clear()
        resolve({ items: [playlist('1', 'Alpha')], total: 1 })
        await loading

        expect(store.playlists).toEqual([])
        expect(store.status).toBe('idle')
    })
})
