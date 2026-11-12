import type { RouteRecordRaw } from 'vue-router'
import EqualizerView from '@/views/EqualizerView.vue'
import HomeView from '@/views/HomeView.vue'
import NotFoundView from '@/views/NotFoundView.vue'
import NowPlayingView from '@/views/NowPlayingView.vue'
import PlaylistsView from '@/views/PlaylistsView.vue'
import ProfileSelectView from '@/views/ProfileSelectView.vue'
import SettingsView from '@/views/SettingsView.vue'
import AdminCacheView from '@/views/admin/AdminCacheView.vue'
import AdminJobsView from '@/views/admin/AdminJobsView.vue'
import AdminLibraryView from '@/views/admin/AdminLibraryView.vue'
import AdminTranscodingView from '@/views/admin/AdminTranscodingView.vue'
import AdminUsersView from '@/views/admin/AdminUsersView.vue'
import AlbumView from '@/views/library/AlbumView.vue'
import ArtistView from '@/views/library/ArtistView.vue'
import GenreView from '@/views/library/GenreView.vue'
import LibraryView from '@/views/library/LibraryView.vue'
import SearchView from '@/views/library/SearchView.vue'
import TrackView from '@/views/library/TrackView.vue'
import YearView from '@/views/library/YearView.vue'
import PlaylistView from '@/views/playlists/PlaylistView.vue'
import RecentlyPlayedView from '@/views/playlists/RecentlyPlayedView.vue'
import {
    defaultLibrarySection,
    librarySectionPattern,
    type MainTab,
} from '@/navigation/libraryTree'

declare module 'vue-router' {
    interface RouteMeta {
        /** Main tab (and bottom navigation item) of the page. */
        tab?: MainTab
        /** Rendered without the app shell (profile selection, internal pages). */
        bare?: boolean
        /** Reachable without a selected profile. */
        public?: boolean
        /** Only for profiles with the role admin. */
        admin?: boolean
    }
}

/**
 * Internal pages that exist only in development mode. The check of import.meta.env.DEV stays
 * literally at the import, so the production build leaves out the page and its chunk.
 */
export const devRoutes: RouteRecordRaw[] = import.meta.env.DEV
    ? [
          {
              path: '/dev/components',
              name: 'dev-components',
              component: () => import('@/views/dev/DevComponentsView.vue'),
              meta: { bare: true, public: true },
          },
      ]
    : []

const librarySongs = { name: 'library-section', params: { section: defaultLibrarySection } }

/**
 * Routes of the web app. The views are imported eagerly: there are only a few small ones, and no
 * chunk of an outdated version has to be loaded after an update of the service worker.
 */
export function createRoutes(extra: readonly RouteRecordRaw[] = []): RouteRecordRaw[] {
    return [
        { path: '/', name: 'home', component: HomeView, meta: { tab: 'home' } },
        {
            path: '/profiles',
            name: 'profiles',
            component: ProfileSelectView,
            meta: { bare: true, public: true },
        },
        {
            path: '/now-playing',
            name: 'now-playing',
            component: NowPlayingView,
            meta: { tab: 'now-playing' },
        },
        {
            path: '/now-playing/equalizer',
            name: 'equalizer',
            component: EqualizerView,
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
            path: '/library/search',
            name: 'library-search',
            component: SearchView,
            meta: { tab: 'library' },
        },
        {
            path: '/library/albums/:id',
            name: 'library-album',
            component: AlbumView,
            props: true,
            meta: { tab: 'library' },
        },
        {
            path: '/library/artists/:id',
            name: 'library-artist',
            component: ArtistView,
            props: true,
            meta: { tab: 'library' },
        },
        {
            path: '/library/genres/:genre',
            name: 'library-genre',
            component: GenreView,
            props: true,
            meta: { tab: 'library' },
        },
        {
            path: '/library/years/:year(\\d{1,4})',
            name: 'library-year',
            component: YearView,
            props: true,
            meta: { tab: 'library' },
        },
        {
            path: '/library/tracks/:id',
            name: 'library-track',
            component: TrackView,
            props: true,
            meta: { tab: 'library' },
        },
        {
            path: '/playlists',
            name: 'playlists',
            component: PlaylistsView,
            meta: { tab: 'playlists' },
        },
        {
            path: '/playlists/new',
            name: 'playlists-new',
            component: PlaylistsView,
            props: { create: true },
            meta: { tab: 'playlists' },
        },
        {
            path: '/playlists/recently-played',
            name: 'playlists-recently-played',
            component: RecentlyPlayedView,
            meta: { tab: 'playlists' },
        },
        {
            path: '/playlists/:id',
            name: 'playlist',
            component: PlaylistView,
            props: true,
            meta: { tab: 'playlists' },
        },
        { path: '/settings', name: 'settings', component: SettingsView },
        { path: '/admin', name: 'admin', redirect: { name: 'admin-library' } },
        {
            path: '/admin/library',
            name: 'admin-library',
            component: AdminLibraryView,
            meta: { admin: true },
        },
        {
            path: '/admin/jobs',
            name: 'admin-jobs',
            component: AdminJobsView,
            meta: { admin: true },
        },
        {
            path: '/admin/cache',
            name: 'admin-cache',
            component: AdminCacheView,
            meta: { admin: true },
        },
        {
            path: '/admin/transcoding',
            name: 'admin-transcoding',
            component: AdminTranscodingView,
            meta: { admin: true },
        },
        {
            path: '/admin/users',
            name: 'admin-users',
            component: AdminUsersView,
            meta: { admin: true },
        },
        ...extra,
        { path: '/:pathMatch(.*)*', name: 'not-found', component: NotFoundView },
    ]
}
