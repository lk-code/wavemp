import type { RouteRecordRaw } from 'vue-router'
import LibraryView from '@/views/LibraryView.vue'
import NotFoundView from '@/views/NotFoundView.vue'
import NowPlayingView from '@/views/NowPlayingView.vue'
import PlaylistsView from '@/views/PlaylistsView.vue'
import {
    defaultLibrarySection,
    librarySectionPattern,
    type MainTab,
} from '@/navigation/libraryTree'

declare module 'vue-router' {
    interface RouteMeta {
        /** Main tab (and bottom navigation item) of the page. */
        tab?: MainTab
        /** Rendered without the app shell (internal pages). */
        bare?: boolean
    }
}

/**
 * Internal pages that exist only in development mode. The check of import.meta.env.DEV stays
 * literally at the import, so the production build leaves out the page and its chunk.
 */
export const devRoutes: RouteRecordRaw[] = []

const librarySongs = { name: 'library-section', params: { section: defaultLibrarySection } }

/**
 * Routes of the web app. The views are imported eagerly: there are only a few small ones, and no
 * chunk of an outdated version has to be loaded after an update of the service worker.
 */
export function createRoutes(extra: readonly RouteRecordRaw[] = []): RouteRecordRaw[] {
    return [
        // The start page follows in step 11; until then the library opens.
        { path: '/', redirect: librarySongs },
        {
            path: '/now-playing',
            name: 'now-playing',
            component: NowPlayingView,
            meta: { tab: 'now-playing' },
        },
        { path: '/library', name: 'library', redirect: librarySongs },
        {
            path: `/library/:section(${librarySectionPattern})`,
            name: 'library-section',
            component: LibraryView,
            props: true,
            meta: { tab: 'library' },
        },
        {
            path: '/playlists',
            name: 'playlists',
            component: PlaylistsView,
            meta: { tab: 'playlists' },
        },
        ...extra,
        { path: '/:pathMatch(.*)*', name: 'not-found', component: NotFoundView },
    ]
}
