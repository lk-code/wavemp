<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import type {
    Album,
    AlbumSort,
    Artist,
    ArtistSort,
    Genre,
    GenreSort,
    Year,
} from '@/api/generated/models'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import FavoritesCollection from '@/components/collection/FavoritesCollection.vue'
import LibraryScopeSwitch from '@/components/collection/LibraryScopeSwitch.vue'
import TileCollection from '@/components/library/TileCollection.vue'
import TrackCollection, { type TrackFilter } from '@/components/library/TrackCollection.vue'
import { isMyMusic } from '@/collection/presentation'
import { useLibraryTiles } from '@/composables/useLibraryTiles'
import type { PagedListStatus } from '@/composables/usePagedList'
import { listAlbums, listArtists, listGenres, listYears } from '@/library/api'
import { queryText } from '@/library/search'
import { parseSort, sortContext } from '@/library/sorting'

/**
 * A section of the library: the songs grouped by album, the recently added songs (newest first),
 * the favorites of the profile, or albums, artists, genres and years as tiles. Search text (?q=,
 * not for years and favorites), sort order (?sort=, ?direction=) and the "my music" filter
 * (?myMusic=true, not for favorites) come from the query of the route.
 */
const props = defineProps<{
    /** Section of the library (route parameter), e.g. "songs". */
    section: string
}>()

const { t } = useI18n()
const route = useRoute()
const tiles = useLibraryTiles()
const status = ref<PagedListStatus>('idle')

// Years and favorites cannot be searched, so their sections ignore a search text and offer none.
const filterable = computed(() => props.section !== 'years' && props.section !== 'favorites')
// The favorites belong to the "my music" anyway, so their section offers no switch.
const scoped = computed(() => props.section !== 'favorites')
const myMusic = computed(() => scoped.value && isMyMusic(route.query))
const trackFilter = computed<TrackFilter>(() => (myMusic.value ? { myMusic: true } : {}))
const search = computed(() => (filterable.value ? queryText(route.query) : ''))
const sortState = computed(() => {
    const list = sortContext({ name: 'library-section', params: { section: props.section } })
    return list ? parseSort(list, route.query, search.value !== '') : {}
})
const reloadKey = computed(() =>
    JSON.stringify([search.value, sortState.value.sort, sortState.value.direction, myMusic.value]),
)
const showAll = computed(() => {
    const query = { ...route.query }
    delete query.q
    return { name: 'library-section', params: { section: props.section }, query }
})

const labels: Record<string, string> = {
    'recently-added': 'library.recentlyAdded',
    favorites: 'library.favorites',
    artists: 'library.artists',
    albums: 'library.albums',
    songs: 'library.songs',
    genres: 'library.genres',
    years: 'library.years',
}
const label = computed(() => t(labels[props.section] ?? 'library.songs'))

function query(offset: number, limit: number) {
    return {
        offset,
        limit,
        q: search.value || undefined,
        direction: sortState.value.direction,
        myMusic: myMusic.value || undefined,
    }
}

function fetchAlbums(offset: number, limit: number) {
    return listAlbums({ ...query(offset, limit), sort: sortState.value.sort as AlbumSort })
}

function fetchArtists(offset: number, limit: number) {
    return listArtists({ ...query(offset, limit), sort: sortState.value.sort as ArtistSort })
}

function fetchGenres(offset: number, limit: number) {
    return listGenres({ ...query(offset, limit), sort: sortState.value.sort as GenreSort })
}

function fetchYears(offset: number, limit: number) {
    return listYears({
        offset,
        limit,
        direction: sortState.value.direction,
        myMusic: myMusic.value || undefined,
    })
}
</script>

<template>
    <div
        data-part="view"
        data-view="library"
        :data-section="section"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <LibraryScopeSwitch v-if="scoped" />
        <template v-if="search">
            <BaseAlert tone="info" :message="t('library.filteredBy', { query: search })" />
            <BaseLink :to="showAll">{{ t('library.showAll') }}</BaseLink>
        </template>
        <TrackCollection
            v-if="section === 'songs'"
            :label="label"
            :filter="trackFilter"
            :search="search"
            :sort="sortState.sort"
            :direction="sortState.direction"
            @status="status = $event"
        />
        <TrackCollection
            v-else-if="section === 'recently-added'"
            :label="label"
            :filter="trackFilter"
            :search="search"
            sort="FirstSeenAt"
            direction="Desc"
            @status="status = $event"
        />
        <FavoritesCollection
            v-else-if="section === 'favorites'"
            :label="label"
            @status="status = $event"
        />
        <TileCollection
            v-else-if="section === 'albums'"
            :label="label"
            :fetch-page="fetchAlbums"
            :to-tile="(item: Album) => tiles.album(item)"
            :reload-key="reloadKey"
            :search="search"
            :filtered="myMusic"
            @status="status = $event"
        />
        <TileCollection
            v-else-if="section === 'artists'"
            :label="label"
            :fetch-page="fetchArtists"
            :to-tile="(item: Artist) => tiles.artist(item)"
            :reload-key="reloadKey"
            :search="search"
            :filtered="myMusic"
            @status="status = $event"
        />
        <TileCollection
            v-else-if="section === 'genres'"
            :label="label"
            :fetch-page="fetchGenres"
            :to-tile="(item: Genre) => tiles.genre(item)"
            :reload-key="reloadKey"
            :search="search"
            :filtered="myMusic"
            @status="status = $event"
        />
        <TileCollection
            v-else-if="section === 'years'"
            :label="label"
            :fetch-page="fetchYears"
            :to-tile="(item: Year) => tiles.year(item)"
            :reload-key="reloadKey"
            :filtered="myMusic"
            @status="status = $event"
        />
    </div>
</template>
