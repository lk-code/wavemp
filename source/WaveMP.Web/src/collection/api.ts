// Requests of the collection of the profile: favorites, ratings and the home page. The only place
// of the web app that calls these endpoints, so components and tests can replace it as a whole
// (vi.mock('@/collection/api')).

import { apiClient } from '@/api/client'
import type { FavoritesRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/me/favorites'
import type { FavoritePage, FavoriteType, Home } from '@/api/generated/models'

export type FavoriteQuery = FavoritesRequestBuilderGetQueryParameters

function favoritesOf(type: FavoriteType) {
    const favorites = apiClient.api.v1.me.favorites
    switch (type) {
        case 'Album':
            return favorites.albums
        case 'Artist':
            return favorites.artists
        default:
            return favorites.tracks
    }
}

export function listFavorites(query: FavoriteQuery): Promise<FavoritePage | undefined> {
    return apiClient.api.v1.me.favorites.get({ queryParameters: query })
}

/** Marks a track, album or artist as favorite; marking it again changes nothing. */
export async function addFavorite(type: FavoriteType, id: string): Promise<void> {
    await favoritesOf(type).byId(id).put()
}

/** Removes a favorite; removing one that is no favorite succeeds as well. */
export async function removeFavorite(type: FavoriteType, id: string): Promise<void> {
    await favoritesOf(type).byId(id).delete()
}

/** Rates a track with 1 to 5 stars; 0 removes the rating. */
export async function setTrackRating(id: string, rating: number): Promise<void> {
    await apiClient.api.v1.tracks.byId(id).rating.put({ rating })
}

/** The rows of the home page, each with up to limit items. */
export function getHome(limit: number): Promise<Home | undefined> {
    return apiClient.api.v1.me.home.get({ queryParameters: { limit } })
}
