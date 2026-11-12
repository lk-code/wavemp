import { useI18n } from 'vue-i18n'
import type { Album, Artist, Genre, Playlist, Track, Year } from '@/api/generated/models'
import type { Tile } from '@/components/library/TileList.vue'
import { useFormat } from '@/composables/useFormat'
import { artistLabelKey } from '@/library/presentation'

/**
 * Tiles of the library lists: albums with cover and album artist, artists, songs with cover and
 * artist, genres, years and playlists.
 */
export function useLibraryTiles() {
    const { t } = useI18n()
    const format = useFormat()

    function songs(count: number | null | undefined): string {
        return t('library.songCount', { count: format.number(count ?? 0) })
    }

    function album(item: Album): Tile {
        const key = artistLabelKey(item.artist, item.isCompilation ?? false)
        return {
            id: item.id ?? '',
            kind: 'album',
            title: item.title ?? '',
            subtitle: key ? t(key) : (item.artist?.name ?? ''),
            to: { name: 'library-album', params: { id: item.id ?? '' } },
            cover: { id: item.id ?? '', available: item.hasCover ?? false },
        }
    }

    function artist(item: Artist): Tile {
        return {
            id: item.id ?? '',
            kind: 'artist',
            title: item.name ?? '',
            subtitle: songs(item.trackCount),
            to: { name: 'library-artist', params: { id: item.id ?? '' } },
        }
    }

    function track(item: Track): Tile {
        const key = artistLabelKey(item.artist)
        return {
            id: item.id ?? '',
            kind: 'track',
            title: item.title ?? '',
            subtitle: key ? t(key) : (item.artist?.name ?? ''),
            to: { name: 'library-track', params: { id: item.id ?? '' } },
            cover: { kind: 'track', id: item.id ?? '', available: item.hasCover ?? false },
        }
    }

    function playlist(item: Playlist): Tile {
        return {
            id: item.id ?? '',
            kind: 'playlist',
            title: item.name ?? '',
            subtitle: songs(item.trackCount),
            to: { name: 'playlist', params: { id: item.id ?? '' } },
            playlistCover: { id: item.id ?? '', cover: item.cover },
        }
    }

    function genre(item: Genre): Tile {
        return {
            id: item.name ?? '',
            kind: 'genre',
            title: item.name ?? '',
            subtitle: songs(item.trackCount),
            to: { name: 'library-genre', params: { genre: item.name ?? '' } },
        }
    }

    function year(item: Year): Tile {
        const value = item.year ?? 0
        return {
            id: String(value),
            kind: 'year',
            title: format.year(value),
            subtitle: songs(item.trackCount),
            to: { name: 'library-year', params: { year: String(value) } },
        }
    }

    return { album, artist, track, genre, year, playlist }
}
