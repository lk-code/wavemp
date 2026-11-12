// Rows of the home page: pure functions that turn the answer of the API into tiles.

import type { Album, Artist, Favorite, Track } from '@/api/generated/models'
import type { Tile } from '@/components/library/TileList.vue'
import { favoriteItem } from './presentation'

/** Items per row of the home page. */
export const homeRowLimit = 20

/** The items that have an id; items without one cannot be shown or linked. */
export function withIds<T extends { id?: string | null }>(
    items: readonly (T | null | undefined)[],
): (T & { id: string })[] {
    return items.filter((item): item is T & { id: string } => !!item?.id)
}

/** The tracks the player can play: those with audio. */
export function playableTracks(tracks: readonly Track[]): Track[] {
    return tracks.filter((track) => track.hasAudio)
}

export interface TileFactory {
    track(item: Track): Tile
    album(item: Album): Tile
    artist(item: Artist): Tile
}

/**
 * The tile of a favorite: a song, an album or an artist. The id names the type as well, since a
 * row mixes them. Favorites whose item disappeared give no tile.
 */
export function favoriteTile(favorite: Favorite, tiles: TileFactory): Tile[] {
    const item = favoriteItem(favorite)
    switch (item?.type) {
        case 'Track':
            return item.track.id
                ? [{ ...tiles.track(item.track), id: `track:${item.track.id}` }]
                : []
        case 'Album':
            return item.album.id
                ? [{ ...tiles.album(item.album), id: `album:${item.album.id}` }]
                : []
        case 'Artist':
            return item.artist.id
                ? [{ ...tiles.artist(item.artist), id: `artist:${item.artist.id}` }]
                : []
        default:
            return []
    }
}
