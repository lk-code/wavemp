import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import { PlaylistSortObject, type Playlist } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'
import {
    addPlaylistEntries,
    createPlaylist,
    deletePlaylist,
    listPlaylists,
    renamePlaylist,
} from '@/playlists/api'
import { useSessionStore } from '@/stores/session'

export type PlaylistsStatus = 'idle' | 'loading' | 'ready' | 'error'

/**
 * The playlists of the current profile, sorted by name, for the sidebar, the overview and the
 * dialog that adds tracks. Changes go through the store, so every place shows the new state;
 * errors are passed on to the caller, which shows them.
 */
export const usePlaylistsStore = defineStore('playlists', () => {
    const session = useSessionStore()
    const playlists = ref<Playlist[]>([])
    const status = ref<PlaylistsStatus>('idle')
    let generation = 0

    const sorted = computed(() =>
        [...playlists.value].sort(
            (a, b) =>
                (a.name ?? '').localeCompare(b.name ?? '', undefined, { sensitivity: 'base' }) ||
                (a.id ?? '').localeCompare(b.id ?? ''),
        ),
    )

    async function load(): Promise<void> {
        const current = ++generation
        status.value = 'loading'
        try {
            const loaded = await fetchAllPages((offset, limit) =>
                listPlaylists({ offset, limit, sort: PlaylistSortObject.Name }),
            )
            if (current === generation) {
                playlists.value = loaded
                status.value = 'ready'
            }
        } catch {
            if (current === generation) {
                status.value = 'error'
            }
        }
    }

    /** Loads the playlists unless they are loaded or loading already. */
    async function ensureLoaded(): Promise<void> {
        if (status.value === 'idle' || status.value === 'error') {
            await load()
        }
    }

    /** Takes over the state of a playlist the API returned. */
    function apply(playlist: Playlist | undefined): void {
        if (!playlist?.id) {
            return
        }

        const index = playlists.value.findIndex((item) => item.id === playlist.id)
        playlists.value =
            index < 0
                ? [...playlists.value, playlist]
                : playlists.value.map((item, position) => (position === index ? playlist : item))
    }

    function forget(id: string): void {
        playlists.value = playlists.value.filter((item) => item.id !== id)
    }

    function find(id: string): Playlist | undefined {
        return playlists.value.find((item) => item.id === id)
    }

    async function create(name: string, trackIds: readonly string[] = []): Promise<Playlist> {
        const created = await createPlaylist(name, trackIds)
        if (!created) {
            throw new Error('The API returned no playlist.')
        }

        apply(created)
        return created
    }

    async function rename(id: string, name: string): Promise<Playlist | undefined> {
        const renamed = await renamePlaylist(id, name)
        apply(renamed)
        return renamed
    }

    async function remove(id: string): Promise<void> {
        await deletePlaylist(id)
        forget(id)
    }

    /** Adds tracks at the end of a playlist and returns the playlist afterwards. */
    async function addTracks(
        id: string,
        trackIds: readonly string[],
    ): Promise<Playlist | undefined> {
        const added = await addPlaylistEntries(id, trackIds)
        apply(added?.playlist ?? undefined)
        return added?.playlist ?? undefined
    }

    /** Forgets everything, e.g. when the profile changes. */
    function clear(): void {
        generation++
        playlists.value = []
        status.value = 'idle'
    }

    // The playlists belong to the profile of the session.
    watch(
        () => session.token,
        () => clear(),
    )

    return {
        playlists: sorted,
        status,
        load,
        ensureLoaded,
        apply,
        forget,
        find,
        create,
        rename,
        remove,
        addTracks,
        clear,
    }
})
