import { describe, expect, it } from 'vitest'
import { nextFocus, visibleItems, type TreeNodeBase } from '../tree'

const nodes: TreeNodeBase[] = [
    { id: 'playlists', children: [{ id: 'create', disabled: true }] },
    {
        id: 'library',
        children: [{ id: 'recent' }, { id: 'artists' }, { id: 'songs' }],
    },
]

const allExpanded = new Set(['playlists', 'library'])

describe('visibleItems', () => {
    it('lists expanded nodes with their children in order', () => {
        expect(visibleItems(nodes, allExpanded).map((item) => item.id)).toEqual([
            'playlists',
            'create',
            'library',
            'recent',
            'artists',
            'songs',
        ])
    })

    it('leaves out the children of collapsed nodes', () => {
        expect(visibleItems(nodes, new Set(['library'])).map((item) => item.id)).toEqual([
            'playlists',
            'library',
            'recent',
            'artists',
            'songs',
        ])
    })

    it('knows level, parent and children', () => {
        const items = visibleItems(nodes, allExpanded)
        expect(items[0]).toEqual({ id: 'playlists', level: 1, parentId: null, hasChildren: true })
        expect(items[3]).toEqual({
            id: 'recent',
            level: 2,
            parentId: 'library',
            hasChildren: false,
        })
    })
})

describe('nextFocus', () => {
    it('moves up and down through the visible items', () => {
        expect(nextFocus('ArrowDown', 'playlists', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'create',
        })
        expect(nextFocus('ArrowDown', 'playlists', nodes, new Set())).toEqual({
            type: 'focus',
            id: 'library',
        })
        expect(nextFocus('ArrowUp', 'recent', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'library',
        })
    })

    it('stops at the ends', () => {
        expect(nextFocus('ArrowUp', 'playlists', nodes, allExpanded)).toBeNull()
        expect(nextFocus('ArrowDown', 'songs', nodes, allExpanded)).toBeNull()
    })

    it('jumps to the first and last item', () => {
        expect(nextFocus('Home', 'songs', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'playlists',
        })
        expect(nextFocus('End', 'playlists', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'songs',
        })
    })

    it('expands a collapsed node or moves to its first child with arrow right', () => {
        expect(nextFocus('ArrowRight', 'library', nodes, new Set())).toEqual({
            type: 'expand',
            id: 'library',
        })
        expect(nextFocus('ArrowRight', 'library', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'recent',
        })
        expect(nextFocus('ArrowRight', 'songs', nodes, allExpanded)).toBeNull()
    })

    it('collapses an expanded node or moves to its parent with arrow left', () => {
        expect(nextFocus('ArrowLeft', 'library', nodes, allExpanded)).toEqual({
            type: 'collapse',
            id: 'library',
        })
        expect(nextFocus('ArrowLeft', 'artists', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'library',
        })
        expect(nextFocus('ArrowLeft', 'library', nodes, new Set())).toBeNull()
    })

    it('ignores other keys and starts at the first item for unknown ids', () => {
        expect(nextFocus('Enter', 'library', nodes, allExpanded)).toBeNull()
        expect(nextFocus('ArrowDown', 'unknown', nodes, allExpanded)).toEqual({
            type: 'focus',
            id: 'playlists',
        })
    })
})
