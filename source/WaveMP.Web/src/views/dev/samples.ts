// Sample data of the component overview (/dev/components) in the shape of the API models. Names
// come from the messages (placeholders.*), so they are recognizable as samples in every language.
// Covers are marked as missing, so the overview never calls the API.

import type { MediaFile, Track } from '@/api/generated/models'
import type { Tile } from '@/components/library/TileList.vue'

type Translate = (key: string, values?: Record<string, unknown>) => string

interface SampleAlbum {
    readonly number: number
    readonly artistNumber: number
    readonly genreNumber: number
    readonly year: number
    readonly tracks: readonly {
        readonly title: number
        readonly seconds: number
        readonly video?: 'with-audio' | 'only'
    }[]
}

const albums: readonly SampleAlbum[] = [
    {
        number: 1,
        artistNumber: 1,
        genreNumber: 1,
        year: 2006,
        tracks: [
            { title: 1, seconds: 225, video: 'with-audio' },
            { title: 2, seconds: 198 },
            { title: 3, seconds: 254 },
            { title: 4, seconds: 187, video: 'only' },
        ],
    },
    {
        number: 2,
        artistNumber: 2,
        genreNumber: 2,
        year: 2007,
        tracks: [
            { title: 5, seconds: 242 },
            { title: 6, seconds: 3725, video: 'with-audio' },
        ],
    },
]

/** Tracks of two sample albums. */
export function sampleTracks(t: Translate): Track[] {
    return albums.flatMap((album) =>
        album.tracks.map((track, index) => {
            const artist = {
                id: `sample-artist-${album.artistNumber}`,
                name: t('placeholders.artist', { number: album.artistNumber }),
            }
            return {
                id: `sample-track-${track.title}`,
                title: t('placeholders.track', { number: track.title }),
                artist,
                album: {
                    id: `sample-album-${album.number}`,
                    title: t('placeholders.album', { number: album.number }),
                    artist,
                    isCompilation: false,
                },
                trackNumber: index + 1,
                discNumber: 1,
                year: album.year,
                genre: t('placeholders.genre', { number: album.genreNumber }),
                durationMilliseconds: track.seconds * 1000,
                hasAudio: track.video !== 'only',
                hasVideo: track.video !== undefined,
                hasCover: false,
            }
        }),
    )
}

/** One tile of every kind. */
export function sampleTiles(t: Translate): Tile[] {
    return [
        {
            id: 'album',
            kind: 'album',
            title: t('placeholders.album', { number: 1 }),
            subtitle: t('placeholders.artist', { number: 1 }),
            to: { name: 'library-album', params: { id: 'sample-album-1' } },
            cover: { id: 'sample-album-1', available: false },
        },
        {
            id: 'artist',
            kind: 'artist',
            title: t('placeholders.artist', { number: 1 }),
            subtitle: t('library.songCount', { count: 4 }),
            to: { name: 'library-artist', params: { id: 'sample-artist-1' } },
        },
        {
            id: 'genre',
            kind: 'genre',
            title: t('placeholders.genre', { number: 1 }),
            subtitle: t('library.songCount', { count: 6 }),
            to: { name: 'library-genre', params: { genre: 'sample' } },
        },
        {
            id: 'year',
            kind: 'year',
            title: '2006',
            subtitle: t('library.songCount', { count: 4 }),
            to: { name: 'library-year', params: { year: '2006' } },
        },
        {
            id: 'playlist',
            kind: 'playlist',
            title: t('placeholders.playlist', { number: 1 }),
            subtitle: t('library.songCount', { count: 3 }),
            to: { name: 'playlist', params: { id: 'sample-playlist-1' } },
        },
        {
            id: 'recently-played',
            kind: 'recently-played',
            title: t('playlists.recentlyPlayed'),
            subtitle: t('playlists.automatic'),
            to: { name: 'playlists-recently-played' },
        },
    ]
}

/** The files of a sample track: lossless audio, a lower quality copy, a video and a picture. */
export function sampleFiles(): MediaFile[] {
    const modifiedAt = new Date(Date.UTC(2026, 10, 1, 9, 30))
    const base = { modifiedAt, isWritable: false, isLossless: false, hasEmbeddedPicture: false }
    return [
        {
            ...base,
            id: 'flac',
            kind: 'Audio',
            relativePath: 'Artist/Album/02 - Song.flac',
            isPrimary: true,
            codec: 'flac',
            bitrate: 912_000,
            sampleRate: 44_100,
            bitDepth: 16,
            channels: 2,
            isLossless: true,
            isWritable: true,
            durationMilliseconds: 225_000,
            size: 25_600_000,
        },
        {
            ...base,
            id: 'mp3',
            kind: 'Audio',
            relativePath: 'Artist/Album/02 - Song.mp3',
            isPrimary: false,
            codec: 'mp3',
            bitrate: 320_000,
            sampleRate: 44_100,
            channels: 2,
            durationMilliseconds: 225_000,
            size: 9_000_000,
            issue: 'file.tags_unreadable',
        },
        {
            ...base,
            id: 'mkv',
            kind: 'Video',
            relativePath: 'Artist/Album/02 - Song.mkv',
            isPrimary: true,
            codec: 'h264',
            width: 1920,
            height: 1080,
            durationMilliseconds: 226_000,
            size: 152_000_000,
        },
        {
            ...base,
            id: 'jpg',
            kind: 'Image',
            relativePath: 'Artist/Album/02 - Song.jpg',
            isPrimary: false,
            width: 600,
            height: 600,
            size: 84_000,
        },
    ]
}
