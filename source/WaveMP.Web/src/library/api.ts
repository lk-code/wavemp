// Requests of the library. The only place of the web app that calls the library endpoints, so
// components and tests can replace it as a whole (vi.mock('@/library/api')).

import { apiClient } from '@/api/client'
import type { AlbumsRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/albums'
import type { TracksRequestBuilderGetQueryParameters as AlbumTrackQuery } from '@/api/generated/api/v1/albums/item/tracks'
import type { ArtistsRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/artists'
import type { AlbumsRequestBuilderGetQueryParameters as ArtistAlbumQuery } from '@/api/generated/api/v1/artists/item/albums'
import type { GenresRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/genres'
import type { TracksRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/tracks'
import type { FilesRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/tracks/item/files'
import type { YearsRequestBuilderGetQueryParameters } from '@/api/generated/api/v1/years'
import type {
    Album,
    AlbumPage,
    Artist,
    ArtistPage,
    GenrePage,
    MediaFilePage,
    SearchResult,
    Track,
    TrackMetadata,
    TrackPage,
    UpdateTrackMetadata,
    YearPage,
} from '@/api/generated/models'
import type { CoverSize } from './covers'

export type TrackQuery = TracksRequestBuilderGetQueryParameters
export type AlbumQuery = AlbumsRequestBuilderGetQueryParameters
export type ArtistQuery = ArtistsRequestBuilderGetQueryParameters
export type GenreQuery = GenresRequestBuilderGetQueryParameters
export type YearQuery = YearsRequestBuilderGetQueryParameters
export type TrackFileQuery = FilesRequestBuilderGetQueryParameters

export function listTracks(query: TrackQuery): Promise<TrackPage | undefined> {
    return apiClient.api.v1.tracks.get({ queryParameters: query })
}

export function getTrack(id: string): Promise<Track | undefined> {
    return apiClient.api.v1.tracks.byId(id).get()
}

export function listTrackFiles(
    id: string,
    query: TrackFileQuery,
): Promise<MediaFilePage | undefined> {
    return apiClient.api.v1.tracks.byId(id).files.get({ queryParameters: query })
}

/** The cover of a track, scaled by the API to fit into a square of the size (96, 300 or 1000). */
export function fetchTrackCover(id: string, size: CoverSize): Promise<ArrayBuffer | undefined> {
    return apiClient.api.v1.tracks.byId(id).cover.get({ queryParameters: { size } })
}

export function getTrackMetadata(id: string): Promise<TrackMetadata | undefined> {
    return apiClient.api.v1.tracks.byId(id).metadata.get()
}

export function updateTrackMetadata(
    id: string,
    changes: UpdateTrackMetadata,
): Promise<TrackMetadata | undefined> {
    return apiClient.api.v1.tracks.byId(id).metadata.patch(changes)
}

export function listAlbums(query: AlbumQuery): Promise<AlbumPage | undefined> {
    return apiClient.api.v1.albums.get({ queryParameters: query })
}

export function getAlbum(id: string): Promise<Album | undefined> {
    return apiClient.api.v1.albums.byId(id).get()
}

export function listAlbumTracks(
    id: string,
    query: AlbumTrackQuery,
): Promise<TrackPage | undefined> {
    return apiClient.api.v1.albums.byId(id).tracks.get({ queryParameters: query })
}

/** The cover of an album, scaled by the API to fit into a square of the size (96, 300 or 1000). */
export function fetchAlbumCover(id: string, size: CoverSize): Promise<ArrayBuffer | undefined> {
    return apiClient.api.v1.albums.byId(id).cover.get({ queryParameters: { size } })
}

export function listArtists(query: ArtistQuery): Promise<ArtistPage | undefined> {
    return apiClient.api.v1.artists.get({ queryParameters: query })
}

export function getArtist(id: string): Promise<Artist | undefined> {
    return apiClient.api.v1.artists.byId(id).get()
}

export function listArtistAlbums(
    id: string,
    query: ArtistAlbumQuery,
): Promise<AlbumPage | undefined> {
    return apiClient.api.v1.artists.byId(id).albums.get({ queryParameters: query })
}

export function listGenres(query: GenreQuery): Promise<GenrePage | undefined> {
    return apiClient.api.v1.genres.get({ queryParameters: query })
}

export function listYears(query: YearQuery): Promise<YearPage | undefined> {
    return apiClient.api.v1.years.get({ queryParameters: query })
}

export function searchLibrary(q: string, limit: number): Promise<SearchResult | undefined> {
    return apiClient.api.v1.search.get({ queryParameters: { q, limit } })
}
