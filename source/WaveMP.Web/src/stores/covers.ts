import { defineStore } from 'pinia'
import { statusOf } from '@/api/problems'
import { fetchAlbumCover, fetchTrackCover } from '@/library/api'
import { defaultCoverSize, type CoverSize } from '@/library/covers'
import { fetchPlaylistCover } from '@/playlists/api'

export type CoverKind = 'album' | 'track' | 'playlist'

/** Covers kept loaded without a component that shows them, the least recently used go first. */
export const maxCachedCovers = 100

interface Entry {
    url: string | null
    loaded: boolean
    pending: Promise<string | null> | null
    refs: number
    used: number
}

/**
 * Covers as object URLs. Pictures of the API need the session token, which an <img> element cannot
 * send, so they are loaded through the API client and shown from a blob. Every size of a cover is
 * an entry of its own. Components acquire a cover while they show it and release it afterwards;
 * requests for the same cover and size are shared. The browser keeps the pictures as well and asks
 * the API with their ETag whether they changed. A missing cover (404) is remembered as null, other
 * errors are not, so the next attempt loads again. Own covers of playlists carry the version the
 * API names, so a new picture gets a new entry instead of the one already loaded.
 */
export const useCoversStore = defineStore('covers', () => {
    const entries = new Map<string, Entry>()
    let clock = 0

    function keyOf(kind: CoverKind, id: string, size: CoverSize, version?: string): string {
        return version ? `${kind}:${id}:${version}:${size}` : `${kind}:${id}:${size}`
    }

    function fetchCover(kind: CoverKind, id: string, size: CoverSize) {
        switch (kind) {
            case 'album':
                return fetchAlbumCover(id, size)
            case 'playlist':
                return fetchPlaylistCover(id, size)
            default:
                return fetchTrackCover(id, size)
        }
    }

    async function load(kind: CoverKind, id: string, size: CoverSize): Promise<string | null> {
        try {
            const buffer = await fetchCover(kind, id, size)
            return buffer && buffer.byteLength > 0 ? URL.createObjectURL(new Blob([buffer])) : null
        } catch (error) {
            if (statusOf(error) === 404) {
                return null
            }

            throw error
        }
    }

    /** Forgets an entry whose cover failed to load once no component shows it any more. */
    function forgetFailed(key: string, entry: Entry): void {
        if (entry.refs === 0 && !entry.loaded && !entry.pending && entries.get(key) === entry) {
            entries.delete(key)
        }
    }

    function evict(): void {
        const idle = [...entries].filter(([, entry]) => entry.refs === 0 && entry.loaded)
        if (idle.length <= maxCachedCovers) {
            return
        }

        idle.sort(([, a], [, b]) => a.used - b.used)
        for (const [key, entry] of idle.slice(0, idle.length - maxCachedCovers)) {
            if (entry.url) {
                URL.revokeObjectURL(entry.url)
            }

            entries.delete(key)
        }
    }

    /**
     * The object URL of a cover in the size, or null if there is none; release it with the same
     * size (and version) when it is not shown.
     */
    function acquire(
        kind: CoverKind,
        id: string,
        size: CoverSize = defaultCoverSize,
        version?: string,
    ): Promise<string | null> {
        const key = keyOf(kind, id, size, version)
        let entry = entries.get(key)
        if (!entry) {
            entry = { url: null, loaded: false, pending: null, refs: 0, used: 0 }
            entries.set(key, entry)
        }

        entry.refs++
        entry.used = ++clock
        if (entry.loaded) {
            return Promise.resolve(entry.url)
        }

        let pending = entry.pending
        if (!pending) {
            const current = entry
            pending = load(kind, id, size).then(
                (url) => {
                    current.url = url
                    current.loaded = true
                    current.pending = null
                    evict()
                    return url
                },
                () => {
                    current.pending = null
                    forgetFailed(key, current)
                    return null
                },
            )
            current.pending = pending
        }

        return pending
    }

    function release(
        kind: CoverKind,
        id: string,
        size: CoverSize = defaultCoverSize,
        version?: string,
    ): void {
        const key = keyOf(kind, id, size, version)
        const entry = entries.get(key)
        if (!entry) {
            return
        }

        entry.refs = Math.max(0, entry.refs - 1)
        entry.used = ++clock
        forgetFailed(key, entry)
        evict()
    }

    /** Number of covers kept, for tests and diagnostics. */
    function size(): number {
        return entries.size
    }

    return { acquire, release, size }
})
