// Sorting of the library lists: the keys each list offers, the defaults (the same as on the
// server) and the sort state in the query of the route (?sort=Title&direction=Desc), so sorted
// lists can be linked.

import type { LocationQuery } from 'vue-router'

export type SortList = 'tracks' | 'albums' | 'artists' | 'genres' | 'years'
export type SortDirection = 'Asc' | 'Desc'

export const relevanceKey = 'Relevance'

/** Sort keys of each list, without the relevance (it needs a search text). */
export const sortKeys: Readonly<Record<SortList, readonly string[]>> = {
    tracks: ['AlbumArtist', 'Album', 'Title', 'Artist', 'Duration', 'Year', 'Genre', 'FirstSeenAt'],
    albums: ['Artist', 'Title', 'Year', 'FirstSeenAt'],
    artists: ['Name', 'FirstSeenAt'],
    genres: ['Name', 'TrackCount'],
    years: [],
}

const defaultKeys: Readonly<Record<SortList, string | undefined>> = {
    tracks: 'AlbumArtist',
    albums: 'Artist',
    artists: 'Name',
    genres: 'Name',
    years: undefined,
}

const descendingKeys = new Set(['FirstSeenAt', relevanceKey, 'TrackCount'])

function supportsRelevance(list: SortList): boolean {
    return list === 'tracks' || list === 'albums' || list === 'artists'
}

/** The keys offered for a list; with a search text the relevance as well. */
export function sortOptions(list: SortList, hasSearch: boolean): string[] {
    return [...sortKeys[list], ...(hasSearch && supportsRelevance(list) ? [relevanceKey] : [])]
}

/** The key the server uses without one: the relevance with a search text. */
export function defaultSort(list: SortList, hasSearch: boolean): string | undefined {
    return hasSearch && supportsRelevance(list) ? relevanceKey : defaultKeys[list]
}

/** The direction the server uses without one: descending for years, recent, relevant, counts. */
export function defaultDirection(list: SortList, sort: string | undefined): SortDirection {
    return list === 'years' || (sort !== undefined && descendingKeys.has(sort)) ? 'Desc' : 'Asc'
}

export interface SortState {
    /** Sort key from the route; undefined for the default. */
    readonly sort?: string
    /** Direction from the route; undefined for the default. */
    readonly direction?: SortDirection
}

function single(value: LocationQuery[string] | undefined): string | undefined {
    const first = Array.isArray(value) ? value[0] : value
    return typeof first === 'string' ? first : undefined
}

/** The sort state of the route; unknown values mean the default. */
export function parseSort(list: SortList, query: LocationQuery, hasSearch: boolean): SortState {
    const sort = single(query.sort)
    const direction = single(query.direction)
    return {
        sort: sort !== undefined && sortOptions(list, hasSearch).includes(sort) ? sort : undefined,
        direction: direction === 'Asc' || direction === 'Desc' ? direction : undefined,
    }
}

/** The key and direction that apply, with the defaults filled in. */
export function effectiveSort(
    list: SortList,
    state: SortState,
    hasSearch: boolean,
): { sort: string | undefined; direction: SortDirection } {
    const sort = state.sort ?? defaultSort(list, hasSearch)
    return { sort, direction: state.direction ?? defaultDirection(list, sort) }
}

export interface SortRoute {
    readonly name?: string | symbol | null
    readonly params: Readonly<Record<string, string | string[] | undefined>>
}

const sectionLists: Readonly<Record<string, SortList>> = {
    songs: 'tracks',
    albums: 'albums',
    artists: 'artists',
    genres: 'genres',
    years: 'years',
}

/**
 * The list that the sort options of the toolbar apply to on the current page, or null where
 * nothing can be sorted: recently added (always newest first), search, album, artist and track.
 */
export function sortContext(route: SortRoute): SortList | null {
    switch (route.name) {
        case 'library-section':
            return sectionLists[String(route.params.section)] ?? null
        case 'library-genre':
        case 'library-year':
            return 'tracks'
        default:
            return null
    }
}

function words(key: string): string[] {
    return key.split(/(?=[A-Z])/).map((word) => word.toLowerCase())
}

/** Id of the menu entry of a sort key, e.g. "sort-album-artist" (data-item). */
export function sortItemId(key: string): string {
    return `sort-${words(key).join('-')}`
}

/** Message key of the name of a sort key, e.g. "sort.keys.albumArtist". */
export function sortLabelKey(key: string): string {
    const [first = '', ...rest] = words(key)
    return `sort.keys.${first}${rest.map((word) => word[0]?.toUpperCase() + word.slice(1)).join('')}`
}
