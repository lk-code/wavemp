// Requests of the playlists. The only place of the web app that calls the playlist endpoints, so
// components and tests can replace it as a whole (vi.mock('@/playlists/api')).

import { apiClient } from '@/api/client'
import type { PlaylistsRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/me/playlists'
import type {
    AddedPlaylistEntries,
    Playlist,
    PlaylistEntry,
    PlaylistEntryPage,
    PlaylistPage,
    RecentlyPlayedTrackPage,
} from '@/api/generated/models'
import type { CoverSize } from '@/library/covers'

export type PlaylistQuery = PlaylistsRequestBuilderGetQueryParameters

function playlist(id: string) {
    return apiClient.api.v1.me.playlists.byId(id)
}

export function listPlaylists(query: PlaylistQuery): Promise<PlaylistPage | undefined> {
    return apiClient.api.v1.me.playlists.get({ queryParameters: query })
}

export function getPlaylist(id: string): Promise<Playlist | undefined> {
    return playlist(id).get()
}

export function createPlaylist(
    name: string,
    trackIds: readonly string[] = [],
): Promise<Playlist | undefined> {
    return apiClient.api.v1.me.playlists.post({ name, trackIds: [...trackIds] })
}

export function renamePlaylist(id: string, name: string): Promise<Playlist | undefined> {
    return playlist(id).patch({ name })
}

export async function deletePlaylist(id: string): Promise<void> {
    await playlist(id).delete()
}

export function listPlaylistEntries(
    id: string,
    offset: number,
    limit: number,
): Promise<PlaylistEntryPage | undefined> {
    return playlist(id).tracks.get({ queryParameters: { offset, limit } })
}

/** Adds tracks at the position (from 0), or at the end without one. */
export function addPlaylistEntries(
    id: string,
    trackIds: readonly string[],
    position?: number,
): Promise<AddedPlaylistEntries | undefined> {
    return playlist(id).tracks.post({ trackIds: [...trackIds], position })
}

export function movePlaylistEntry(
    id: string,
    entryId: string,
    position: number,
): Promise<PlaylistEntry | undefined> {
    return playlist(id).tracks.byEntryId(entryId).patch({ position })
}

export async function removePlaylistEntry(id: string, entryId: string): Promise<void> {
    await playlist(id).tracks.byEntryId(entryId).delete()
}

/** The own cover of a playlist as square WebP in the size (96, 300 or 1000). */
export function fetchPlaylistCover(id: string, size: CoverSize): Promise<ArrayBuffer | undefined> {
    return playlist(id).cover.get({ queryParameters: { size } })
}

/** Uploads a JPEG, PNG or WebP picture as own cover; the server crops it to a square. */
export function uploadPlaylistCover(
    id: string,
    picture: ArrayBuffer,
    contentType: string,
): Promise<Playlist | undefined> {
    return playlist(id).cover.put(picture, contentType)
}

export async function deletePlaylistCover(id: string): Promise<void> {
    await playlist(id).cover.delete()
}

export function listRecentlyPlayed(
    offset: number,
    limit: number,
): Promise<RecentlyPlayedTrackPage | undefined> {
    return apiClient.api.v1.me.recentlyPlayed.get({ queryParameters: { offset, limit } })
}
