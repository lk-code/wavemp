// Pure helpers of the playlists: the order of entries, the slides of a cover made of track covers
// and the checks of a picture before it is uploaded as cover.

import { PlaylistCoverKindObject, type PlaylistCover } from '@/api/generated/models'

/** Longest name of a playlist (the API checks it as well). */
export const playlistNameMaxLength = 100

/** Picture types the API accepts as own cover of a playlist. */
export const coverUploadTypes = ['image/jpeg', 'image/png', 'image/webp'] as const

/** Largest picture the API accepts by default (10 MiB); larger ones are refused before uploading. */
export const maxCoverUploadBytes = 10 * 1024 * 1024

/** Time each track cover of a playlist cover stays before the next one fades in. */
export const coverSlideMilliseconds = 6000

export type CoverUploadProblem = 'type' | 'size'

/** Why a file cannot become the cover of a playlist, or null if it can. */
export function coverUploadProblem(file: {
    readonly type: string
    readonly size: number
}): CoverUploadProblem | null {
    if (!(coverUploadTypes as readonly string[]).includes(file.type)) {
        return 'type'
    }

    return file.size > maxCoverUploadBytes ? 'size' : null
}

/** A copy of the list with the item at from moved to to; indexes outside the list change nothing. */
export function moveItem<T>(items: readonly T[], from: number, to: number): T[] {
    const copy = [...items]
    if (from === to || from < 0 || to < 0 || from >= copy.length || to >= copy.length) {
        return copy
    }

    const [item] = copy.splice(from, 1)
    copy.splice(to, 0, item!)
    return copy
}

/** What a playlist shows as cover: its own picture, the covers of some tracks or the placeholder. */
export type PlaylistCoverView =
    | { readonly kind: 'custom'; readonly version: string }
    | { readonly kind: 'tracks'; readonly trackIds: readonly string[] }
    | { readonly kind: 'none' }

export function coverViewOf(cover: PlaylistCover | null | undefined): PlaylistCoverView {
    if (cover?.kind === PlaylistCoverKindObject.Custom && cover.version) {
        return { kind: 'custom', version: cover.version }
    }

    const trackIds = (cover?.trackIds ?? []).filter((id): id is string => !!id)
    return cover?.kind === PlaylistCoverKindObject.Tracks && trackIds.length > 0
        ? { kind: 'tracks', trackIds }
        : { kind: 'none' }
}

/** The slide after the current one, starting over after the last. */
export function nextSlide(current: number, count: number): number {
    return count <= 0 ? 0 : (current + 1) % count
}
