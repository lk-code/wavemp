import { describe, expect, it } from 'vitest'
import {
    buildLibraryTree,
    librarySections,
    libraryTree,
    mainNavigation,
    playlistNodeId,
} from '../libraryTree'

describe('buildLibraryTree', () => {
    it('leads to creating a playlist and to the recently played tracks', () => {
        const playlists = libraryTree.find((node) => node.id === 'playlists')!

        expect(playlists.children?.map((node) => [node.id, node.to, node.disabled])).toEqual([
            ['create-playlist', { name: 'playlists-new' }, undefined],
            ['recently-played', { name: 'playlists-recently-played' }, undefined],
        ])
    })

    it('lists the playlists of the profile below "Playlists" in their order', () => {
        const tree = buildLibraryTree([
            { id: 'b', name: 'Bravo' },
            { id: null, name: 'Without id' },
            { id: 'a', name: 'Alpha' },
        ])

        const children = tree.find((node) => node.id === 'playlists')!.children!
        expect(children.slice(2)).toEqual([
            {
                id: 'playlist:b',
                labelKey: 'sidebar.playlist',
                label: 'Bravo',
                icon: 'tree-playlist',
                to: { name: 'playlist', params: { id: 'b' } },
            },
            {
                id: 'playlist:a',
                labelKey: 'sidebar.playlist',
                label: 'Alpha',
                icon: 'tree-playlist',
                to: { name: 'playlist', params: { id: 'a' } },
            },
        ])
        expect(tree.find((node) => node.id === 'library')).toBe(
            libraryTree.find((node) => node.id === 'library'),
        )
    })

    it('keeps the fixed tree without playlists', () => {
        expect(buildLibraryTree([])).toEqual(libraryTree)
        expect(playlistNodeId('x')).toBe('playlist:x')
    })
})

describe('main navigation', () => {
    it('starts with the home page', () => {
        expect(mainNavigation.map((item) => item.id)).toEqual([
            'home',
            'now-playing',
            'library',
            'playlists',
        ])
        expect(mainNavigation[0]).toMatchObject({ icon: 'home', to: { name: 'home' } })
    })

    it('offers the favorites as a section of the library', () => {
        expect(librarySections.map((section) => section.id)).toContain('favorites')
        const library = libraryTree.find((node) => node.id === 'library')!
        expect(library.children?.find((node) => node.id === 'favorites')).toMatchObject({
            icon: 'tree-favorites',
            to: { name: 'library-section', params: { section: 'favorites' } },
        })
    })
})
