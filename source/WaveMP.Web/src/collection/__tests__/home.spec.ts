import { describe, expect, it } from 'vitest'
import type { Album, Artist, Track } from '@/api/generated/models'
import type { Tile } from '@/components/library/TileList.vue'
import { favoriteTile, playableTracks, withIds, type TileFactory } from '../home'

const tiles: TileFactory = {
    track: (item: Track): Tile => ({
        id: item.id ?? '',
        kind: 'track',
        title: item.title ?? '',
        to: { name: 'library-track', params: { id: item.id ?? '' } },
    }),
    album: (item: Album): Tile => ({
        id: item.id ?? '',
        kind: 'album',
        title: item.title ?? '',
        to: { name: 'library-album', params: { id: item.id ?? '' } },
    }),
    artist: (item: Artist): Tile => ({
        id: item.id ?? '',
        kind: 'artist',
        title: item.name ?? '',
        to: { name: 'library-artist', params: { id: item.id ?? '' } },
    }),
}

describe('home rows', () => {
    it('keep only items with an id', () => {
        expect(withIds([{ id: 'a' }, null, { id: '' }, undefined, { id: 'b' }])).toEqual([
            { id: 'a' },
            { id: 'b' },
        ])
    })

    it('play only songs with audio', () => {
        const tracks: Track[] = [
            { id: 'a', hasAudio: true },
            { id: 'b', hasAudio: false, hasVideo: true },
        ]
        expect(playableTracks(tracks).map((track) => track.id)).toEqual(['a'])
    })

    it('mix favorites of every type with ids that name the type', () => {
        const same = 'same-id'
        const result = [
            { type: 'Track' as const, track: { id: same, title: 'Song' } },
            { type: 'Album' as const, album: { id: same, title: 'Album' } },
            { type: 'Artist' as const, artist: { id: same, name: 'Band' } },
            { type: 'Album' as const, album: null },
        ].flatMap((favorite) => favoriteTile(favorite, tiles))

        expect(result.map((tile) => [tile.id, tile.kind, tile.title])).toEqual([
            [`track:${same}`, 'track', 'Song'],
            [`album:${same}`, 'album', 'Album'],
            [`artist:${same}`, 'artist', 'Band'],
        ])
    })
})
