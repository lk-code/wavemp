// Location of the current page: breadcrumb, page title and the active entry of the sidebar tree.

import type { RouteLocationRaw } from 'vue-router'
import { findLibrarySection } from './libraryTree'

export interface Crumb {
    readonly id: string
    readonly labelKey: string
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
    'admin-users': 'adminUsers.title',
} as const

export function buildBreadcrumb(route: RouteInfo): Crumb[] {
    const crumbs: Crumb[] = [{ id: 'music', labelKey: 'breadcrumb.music', to: { name: 'library' } }]

    switch (route.name) {
        case 'now-playing':
            crumbs.push({ id: 'now-playing', labelKey: 'nav.nowPlaying' })
            break
        case 'playlists':
            crumbs.push({ id: 'playlists', labelKey: 'nav.playlists' })
            break
        case 'library-section': {
            crumbs.push({ id: 'library', labelKey: 'nav.library', to: { name: 'library' } })
            const section = findLibrarySection(route.params.section)
            if (section) {
                crumbs.push({ id: section.id, labelKey: section.labelKey })
            }
            break
        }
        case 'settings':
            crumbs.push({ id: 'settings', labelKey: 'settings.title' })
            break
        case 'admin-library':
        case 'admin-jobs':
        case 'admin-users':
            crumbs.push({ id: 'admin', labelKey: 'admin.title', to: { name: 'admin' } })
            crumbs.push({ id: route.name, labelKey: adminPageTitles[route.name] })
            break
        case 'not-found':
            crumbs.push({ id: 'not-found', labelKey: 'notFound.title' })
            break
    }

    return crumbs.map((crumb, index) =>
        index === crumbs.length - 1 ? { id: crumb.id, labelKey: crumb.labelKey } : crumb,
    )
}

/** Message key of the page title: the last crumb. */
export function pageTitleKey(route: RouteInfo): string {
    const crumbs = buildBreadcrumb(route)
    return crumbs[crumbs.length - 1]?.labelKey ?? 'app.name'
}

/** Id of the sidebar tree node that belongs to the current page. */
export function activeTreeId(route: RouteInfo): string | undefined {
    if (route.name === 'library-section') {
        return findLibrarySection(route.params.section)?.id
    }

    return route.name === 'playlists' ? 'playlists' : undefined
}
