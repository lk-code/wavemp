// Location of the current page: breadcrumb, page title and the active entry of the sidebar tree.

import type { RouteLocationRaw } from 'vue-router'
import { findLibrarySection, playlistNodeId, type LibrarySection } from './libraryTree'

export interface Crumb {
    readonly id: string
    readonly labelKey: string
    /** Text to show instead of the message, e.g. the name of an album. */
    readonly label?: string
    /** Target of the crumb; the last crumb is the current page and has none. */
    readonly to?: RouteLocationRaw
}

export interface RouteInfo {
    readonly name?: string | symbol | null
    readonly params: Readonly<Record<string, string | string[] | undefined>>
}

/** Message keys of the titles of the admin pages. */
const adminPageTitles = {
    'admin-library': 'adminLibrary.title',
    'admin-jobs': 'adminJobs.title',
    'admin-cache': 'adminCache.title',
    'admin-transcoding': 'adminTranscoding.title',
    'admin-users': 'adminUsers.title',
} as const

/** The section of the library each detail page belongs to. */
const detailSections: Readonly<Record<string, LibrarySection>> = {
    'library-album': 'albums',
    'library-artist': 'artists',
    'library-genre': 'genres',
    'library-year': 'years',
    'library-track': 'songs',
}

/**
 * The crumbs of a page. Detail pages (album, artist, genre, year, track, playlist) end with their
 * name, once it is known; until then with their section.
 */
export function buildBreadcrumb(route: RouteInfo, detailTitle?: string | null): Crumb[] {
    const crumbs: Crumb[] = [{ id: 'music', labelKey: 'breadcrumb.music', to: { name: 'library' } }]
    const library: Crumb = { id: 'library', labelKey: 'nav.library', to: { name: 'library' } }
    const playlistsCrumb: Crumb = {
        id: 'playlists',
        labelKey: 'nav.playlists',
        to: { name: 'playlists' },
    }

    switch (route.name) {
        case 'home':
            crumbs.push({ id: 'home', labelKey: 'nav.home' })
            break
        case 'now-playing':
            crumbs.push({ id: 'now-playing', labelKey: 'nav.nowPlaying' })
            break
        case 'equalizer':
            crumbs.push({
                id: 'now-playing',
                labelKey: 'nav.nowPlaying',
                to: { name: 'now-playing' },
            })
            crumbs.push({ id: 'equalizer', labelKey: 'nav.equalizer' })
            break
        case 'playlists':
        case 'playlists-new':
            crumbs.push({ id: 'playlists', labelKey: 'nav.playlists' })
            break
        case 'playlists-recently-played':
            crumbs.push(playlistsCrumb)
            crumbs.push({ id: 'recently-played', labelKey: 'playlists.recentlyPlayed' })
            break
        case 'playlist':
            crumbs.push(playlistsCrumb)
            if (detailTitle) {
                crumbs.push({ id: 'playlist', labelKey: 'nav.playlists', label: detailTitle })
            }
            break
        case 'library-section': {
            crumbs.push(library)
            const section = findLibrarySection(route.params.section)
            if (section) {
                crumbs.push({ id: section.id, labelKey: section.labelKey })
            }
            break
        }
        case 'library-album':
        case 'library-artist':
        case 'library-genre':
        case 'library-year':
        case 'library-track': {
            crumbs.push(library)
            const section = findLibrarySection(detailSections[route.name])
            if (section) {
                crumbs.push({
                    id: section.id,
                    labelKey: section.labelKey,
                    to: { name: 'library-section', params: { section: section.id } },
                })
                if (detailTitle) {
                    crumbs.push({ id: route.name, labelKey: section.labelKey, label: detailTitle })
                }
            }
            break
        }
        case 'library-search':
            crumbs.push(library)
            crumbs.push({ id: 'search', labelKey: 'search.title' })
            break
        case 'settings':
            crumbs.push({ id: 'settings', labelKey: 'settings.title' })
            break
        case 'admin-library':
        case 'admin-jobs':
        case 'admin-cache':
        case 'admin-transcoding':
        case 'admin-users':
            crumbs.push({ id: 'admin', labelKey: 'admin.title', to: { name: 'admin' } })
            crumbs.push({ id: route.name, labelKey: adminPageTitles[route.name] })
            break
        case 'not-found':
            crumbs.push({ id: 'not-found', labelKey: 'notFound.title' })
            break
    }

    return crumbs.map((crumb, index) =>
        index === crumbs.length - 1
            ? {
                  id: crumb.id,
                  labelKey: crumb.labelKey,
                  ...(crumb.label ? { label: crumb.label } : {}),
              }
            : crumb,
    )
}

/** Message key of the page title: the last crumb (detail pages show their name instead). */
export function pageTitleKey(route: RouteInfo): string {
    const crumbs = buildBreadcrumb(route)
    return crumbs[crumbs.length - 1]?.labelKey ?? 'app.name'
}

/** Id of the sidebar tree node that belongs to the current page. */
export function activeTreeId(route: RouteInfo): string | undefined {
    if (route.name === 'library-section') {
        return findLibrarySection(route.params.section)?.id
    }

    if (typeof route.name === 'string' && route.name in detailSections) {
        return detailSections[route.name]
    }

    switch (route.name) {
        case 'playlists':
            return 'playlists'
        case 'playlists-new':
            return 'create-playlist'
        case 'playlists-recently-played':
            return 'recently-played'
        case 'playlist':
            return typeof route.params.id === 'string' ? playlistNodeId(route.params.id) : undefined
        default:
            return undefined
    }
}
