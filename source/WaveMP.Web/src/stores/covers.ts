import { defineStore } from 'pinia'
import { statusOf } from '@/api/problems'
import { fetchAlbumCover, fetchTrackCover } from '@/library/api'

export type CoverKind = 'album' | 'track'

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
 * send, so they are loaded through the API client and shown from a blob. Components acquire a
 * cover while they show it and release it afterwards; requests for the same cover are shared. A
 * missing cover (404) is remembered as null, other errors are not, so the next attempt loads again.
 */
export const useCoversStore = defineStore('covers', () => {
    const entries = new Map<string, Entry>()
    let clock = 0

    function keyOf(kind: CoverKind, id: string): string {
        return `${kind}:${id}`
    }

    async function load(kind: CoverKind, id: string): Promise<string | null> {
        try {
            const buffer = kind === 'album' ? await fetchAlbumCover(id) : await fetchTrackCover(id)
            return buffer && buffer.byteLength > 0 ? URL.createObjectURL(new Blob([buffer])) : null
        } catch (error) {
            if (statusOf(error) === 404) {
                return null
            }

            throw error
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

    /** The object URL of a cover, or null if there is none; release it when it is not shown. */
    function acquire(kind: CoverKind, id: string): Promise<string | null> {
        const key = keyOf(kind, id)
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
            pending = load(kind, id).then(
                (url) => {
                    current.url = url
                    current.loaded = true
                    current.pending = null
                    evict()
                    return url
                },
                () => {
                    current.pending = null
                    return null
                },
            )
            current.pending = pending
        }

        return pending
    }

    function release(kind: CoverKind, id: string): void {
        const entry = entries.get(keyOf(kind, id))
        if (!entry) {
            return
        }

        entry.refs = Math.max(0, entry.refs - 1)
        entry.used = ++clock
        evict()
    }

    /** Number of covers kept, for tests and diagnostics. */
    function size(): number {
        return entries.size
    }

    return { acquire, release, size }
})
