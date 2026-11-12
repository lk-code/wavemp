// Main navigation (tabs and bottom navigation) and the navigation tree of the sidebar.

import type { RouteLocationRaw } from 'vue-router'
import type { IconSlotName } from '@/themes/iconSlots'

export type MainTab = 'now-playing' | 'library' | 'playlists'

export interface MainNavigationItem {
    readonly id: MainTab
    readonly labelKey: string
    readonly icon: IconSlotName
    readonly to: RouteLocationRaw
}

export const mainNavigation: readonly MainNavigationItem[] = [
    {
        id: 'now-playing',
        labelKey: 'nav.nowPlaying',
        icon: 'now-playing',
        to: { name: 'now-playing' },
    },
    { id: 'library', labelKey: 'nav.library', icon: 'library', to: { name: 'library' } },
    { id: 'playlists', labelKey: 'nav.playlists', icon: 'playlists', to: { name: 'playlists' } },
]

export const librarySections = [
    { id: 'recently-added', labelKey: 'library.recentlyAdded', icon: 'tree-recently-added' },
    { id: 'artists', labelKey: 'library.artists', icon: 'tree-artists' },
    { id: 'albums', labelKey: 'library.albums', icon: 'tree-albums' },
    { id: 'songs', labelKey: 'library.songs', icon: 'tree-songs' },
    { id: 'genres', labelKey: 'library.genres', icon: 'tree-genres' },
    { id: 'years', labelKey: 'library.years', icon: 'tree-years' },
] as const satisfies readonly { id: string; labelKey: string; icon: IconSlotName }[]

export type LibrarySection = (typeof librarySections)[number]['id']

export const defaultLibrarySection: LibrarySection = 'songs'

/** Route pattern of the library sections, e.g. "recently-added|artists|...". */
export const librarySectionPattern = librarySections.map((section) => section.id).join('|')

export function findLibrarySection(id: unknown) {
    return librarySections.find((section) => section.id === id)
}

export interface NavigationTreeNode {
    readonly id: string
    readonly labelKey: string
    /** Text to show instead of the message, e.g. the name of a playlist. */
    readonly label?: string
    readonly icon: IconSlotName
    readonly to?: RouteLocationRaw
    readonly disabled?: boolean
    readonly children?: readonly NavigationTreeNode[]
}

/** Tree of the sidebar without the playlists of the profile (see buildLibraryTree). */
export const libraryTree: readonly NavigationTreeNode[] = [
    {
        id: 'playlists',
        labelKey: 'sidebar.playlists',
        icon: 'tree-playlists',
        to: { name: 'playlists' },
        children: [
            {
                id: 'create-playlist',
                labelKey: 'sidebar.createPlaylist',
                icon: 'tree-create-playlist',
                to: { name: 'playlists-new' },
            },
            {
                id: 'recently-played',
                labelKey: 'playlists.recentlyPlayed',
                icon: 'tree-recently-played',
                to: { name: 'playlists-recently-played' },
            },
        ],
    },
    {
        id: 'library',
        labelKey: 'sidebar.library',
        icon: 'tree-library',
        to: { name: 'library' },
        children: librarySections.map((section) => ({
            id: section.id,
            labelKey: section.labelKey,
            icon: section.icon,
            to: { name: 'library-section', params: { section: section.id } },
        })),
    },
]

/** Nodes that are expanded when the app starts. */
export const initiallyExpanded: readonly string[] = ['playlists', 'library']

/** Id of the tree node of a playlist. */
export function playlistNodeId(id: string): string {
    return `playlist:${id}`
}

/** The tree of the sidebar with the playlists of the profile below "Playlists", in their order. */
export function buildLibraryTree(
    playlists: readonly { readonly id?: string | null; readonly name?: string | null }[],
): NavigationTreeNode[] {
    const playlistNodes: NavigationTreeNode[] = playlists
        .filter((playlist): playlist is { id: string; name?: string | null } => !!playlist.id)
        .map((playlist) => ({
            id: playlistNodeId(playlist.id),
            labelKey: 'sidebar.playlist',
            label: playlist.name ?? '',
            icon: 'tree-playlist',
            to: { name: 'playlist', params: { id: playlist.id } },
        }))

    return libraryTree.map((node) =>
        node.id === 'playlists'
            ? { ...node, children: [...(node.children ?? []), ...playlistNodes] }
            : node,
    )
}
