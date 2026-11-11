// Groups of tracks for the list grouped by album. Pure functions only; the views translate the
// texts (unknown album, discs).

import type { ArtistReference, Track } from '@/api/generated/models'

/** Cover shown for a group: the cover of its album, or of one of its tracks without album. */
export interface GroupCover {
    readonly kind: 'album' | 'track'
    readonly id: string
    readonly available: boolean
}

export interface TrackGroup {
    /** Stable key: the album id, otherwise the artist of tracks without album. */
    readonly id: string
    readonly albumId?: string
    /** Album title; null for tracks without album (the view shows "Unknown album"). */
    readonly title: string | null
    /** Disc number of a group of one disc of an album (the view shows "Disc n"). */
    readonly disc?: number
    /** Album artist, for tracks without album the artist of the first track. */
    readonly artist: ArtistReference | null
    readonly isCompilation: boolean
    readonly year: number | null
    readonly genre: string | null
    readonly cover: GroupCover | null
    readonly tracks: readonly Track[]
}

function groupKey(track: Track): string {
    if (track.album?.id) {
        return `album:${track.album.id}`
    }

    return track.artist?.id ? `artist:${track.artist.id}` : 'unknown'
}

function createGroup(key: string, track: Track): TrackGroup {
    const album = track.album
    return {
        id: key,
        albumId: album?.id ?? undefined,
        title: album?.title ?? null,
        artist: album ? (album.artist ?? null) : (track.artist ?? null),
        isCompilation: album?.isCompilation ?? false,
        year: track.year ?? null,
        genre: track.genre ?? null,
        cover: null,
        tracks: [],
    }
}

function coverOf(group: TrackGroup, tracks: readonly Track[]): GroupCover | null {
    const withCover = tracks.find((track) => track.hasCover)
    if (group.albumId) {
        return { kind: 'album', id: group.albumId, available: !!withCover }
    }

    return withCover?.id ? { kind: 'track', id: withCover.id, available: true } : null
}

/**
 * Groups consecutive tracks of the same album; tracks without album are grouped by their artist.
 * The order of the tracks stays, so a list sorted by album gets one group per album. Year and genre
 * of a group come from its first track that has one.
 */
export function groupTracksByAlbum(tracks: readonly Track[]): TrackGroup[] {
    const groups: { group: TrackGroup; tracks: Track[] }[] = []
    for (const track of tracks) {
        const key = groupKey(track)
        const last = groups[groups.length - 1]
        if (last && last.group.id === key) {
            last.tracks.push(track)
        } else {
            groups.push({ group: createGroup(key, track), tracks: [track] })
        }
    }

    // Keys repeat when an album appears twice in a list sorted otherwise; the ids must stay unique.
    const seen = new Map<string, number>()
    return groups.map(({ group, tracks: groupTracks }) => {
        const count = seen.get(group.id) ?? 0
        seen.set(group.id, count + 1)
        return {
            ...group,
            id: count === 0 ? group.id : `${group.id}:${count}`,
            year: groupTracks.find((track) => track.year)?.year ?? null,
            genre: groupTracks.find((track) => track.genre)?.genre ?? null,
            cover: coverOf(group, groupTracks),
            tracks: groupTracks,
        }
    })
}

/**
 * The tracks of an album page: one group with the album title, or one group per disc as soon as
 * the album has more than one disc.
 */
export function groupTracksByDisc(tracks: readonly Track[], albumTitle: string): TrackGroup[] {
    const discs = [...new Set(tracks.map((track) => track.discNumber ?? 1))]
    const base: Omit<TrackGroup, 'id' | 'tracks'> = {
        title: albumTitle,
        artist: null,
        isCompilation: false,
        year: null,
        genre: null,
        cover: null,
    }

    if (discs.length < 2) {
        return [{ ...base, id: 'album', tracks }]
    }

    return discs
        .sort((a, b) => a - b)
        .map((disc) => ({
            ...base,
            id: `disc:${disc}`,
            disc,
            tracks: tracks.filter((track) => (track.discNumber ?? 1) === disc),
        }))
}
