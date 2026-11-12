// Presentation of the collection of the profile: ratings, favorites and the rows of the home page.
// Pure functions only.

import type { LocationQuery } from 'vue-router'
import type { Favorite } from '@/api/generated/models'

export const minRating = 0
export const maxRating = 5

/** The stars a track can have, 1 to 5. */
export const ratingStars: readonly number[] = [1, 2, 3, 4, 5]

/** Whether the value is a rating the API accepts: 0 (no rating) to 5 stars. */
export function isValidRating(value: number): boolean {
    return Number.isInteger(value) && value >= minRating && value <= maxRating
}

/** The rating after a click on a star: the same star again removes the rating. */
export function nextRating(current: number, star: number): number {
    return current === star ? 0 : star
}

/** data-state tokens of a star: "on" up to the rating, otherwise "off". */
export function starState(rating: number, star: number): 'on' | 'off' {
    return star <= rating ? 'on' : 'off'
}

/** The track, album or artist of a favorite, as a tile or row; null if it disappeared. */
export function favoriteItem(favorite: Favorite) {
    switch (favorite.type) {
        case 'Track':
            return favorite.track ? { type: 'Track' as const, track: favorite.track } : null
        case 'Album':
            return favorite.album ? { type: 'Album' as const, album: favorite.album } : null
        case 'Artist':
            return favorite.artist ? { type: 'Artist' as const, artist: favorite.artist } : null
        default:
            return null
    }
}

/** Whether the query of a route asks for the "my music" of the profile (?myMusic=true). */
export function isMyMusic(query: LocationQuery): boolean {
    const value = Array.isArray(query.myMusic) ? query.myMusic[0] : query.myMusic
    return value === 'true'
}

/** The query with or without the "my music" filter; the other parameters stay. */
export function withMyMusic(query: LocationQuery, myMusic: boolean): LocationQuery {
    const result: LocationQuery = { ...query }
    if (myMusic) {
        result.myMusic = 'true'
    } else {
        delete result.myMusic
    }
    return result
}
