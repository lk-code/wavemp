<script setup lang="ts">
import { computed, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Album, Artist, Favorite, FavoriteType, Track } from '@/api/generated/models'
import BaseButton from '@/components/base/BaseButton.vue'
import LoadMore from '@/components/library/LoadMore.vue'
import TileList from '@/components/library/TileList.vue'
import PlaylistTrackList, {
    type PlaylistTrackRow,
} from '@/components/playlists/PlaylistTrackList.vue'
import { listFavorites } from '@/collection/api'
import { useFormat } from '@/composables/useFormat'
import { useLibraryTiles } from '@/composables/useLibraryTiles'
import { usePagedList, type PagedListStatus } from '@/composables/usePagedList'

/**
 * The favorites of the profile, the newest first: the songs as a list (with the time they were
 * marked), the albums and the artists as tiles, each loaded page by page. Parts without favorites
 * are left out; without any favorite the page explains how to mark them.
 */
defineProps<{
    /** Accessible name of the list of songs. */
    label: string
}>()

const emit = defineEmits<{
    status: [status: PagedListStatus]
}>()

const { t } = useI18n()
const format = useFormat()
const tiles = useLibraryTiles()
const tracksTitleId = useId()
const albumsTitleId = useId()
const artistsTitleId = useId()

function favoritesOf(type: FavoriteType) {
    return usePagedList<Favorite>((offset, limit) => listFavorites({ offset, limit, type }))
}

const tracks = favoritesOf('Track')
const albums = favoritesOf('Album')
const artists = favoritesOf('Artist')
const lists = [tracks, albums, artists]

const trackRows = computed<PlaylistTrackRow[]>(() =>
    tracks.items.value
        .filter((favorite): favorite is Favorite & { track: Track } => !!favorite.track?.id)
        .map((favorite) => ({
            id: favorite.track.id!,
            track: favorite.track,
            detail: favorite.addedAt
                ? format.date(favorite.addedAt, { dateStyle: 'medium', timeStyle: 'short' })
                : '',
        })),
)
const albumTiles = computed(() =>
    albums.items.value
        .map((favorite) => favorite.album)
        .filter((album): album is Album => !!album?.id)
        .map((album) => tiles.album(album)),
)
const artistTiles = computed(() =>
    artists.items.value
        .map((favorite) => favorite.artist)
        .filter((artist): artist is Artist => !!artist?.id)
        .map((artist) => tiles.artist(artist)),
)

const status = computed<PagedListStatus>(() => {
    const states = lists.map((list) => list.status.value)
    if (states.includes('error')) {
        return 'error'
    }
    if (states.includes('loading') || states.includes('idle')) {
        return 'loading'
    }
    return 'ready'
})
const empty = computed(
    () => status.value === 'ready' && lists.every((list) => list.items.value.length === 0),
)

function reload(): void {
    for (const list of lists) {
        void list.reload()
    }
}

reload()
watch(status, (value) => emit('status', value), { immediate: true })
</script>

<template>
    <div v-if="status === 'error'" data-part="empty-state">
        <p data-part="empty-state-title">{{ t('library.loadError') }}</p>
        <BaseButton :label="t('library.retry')" @click="reload" />
    </div>
    <div v-else-if="empty" data-part="empty-state">
        <p data-part="empty-state-title">{{ t('collection.favoritesEmptyTitle') }}</p>
        <p data-part="empty-state-text">{{ t('collection.favoritesEmptyText') }}</p>
    </div>
    <template v-else>
        <section
            v-if="trackRows.length > 0"
            data-part="section"
            data-item="tracks"
            :aria-labelledby="tracksTitleId"
        >
            <h2 :id="tracksTitleId" data-part="section-title">{{ t('library.songsTitle') }}</h2>
            <PlaylistTrackList
                :rows="trackRows"
                :label="label"
                :detail-label="t('collection.addedAt')"
            />
            <LoadMore
                v-if="tracks.hasMore.value"
                :loading="tracks.loadingMore.value"
                @load="tracks.loadMore()"
            />
        </section>
        <section
            v-if="albumTiles.length > 0"
            data-part="section"
            data-item="albums"
            :aria-labelledby="albumsTitleId"
        >
            <h2 :id="albumsTitleId" data-part="section-title">{{ t('library.albumsTitle') }}</h2>
            <TileList :tiles="albumTiles" :label="t('library.albumsTitle')" />
            <LoadMore
                v-if="albums.hasMore.value"
                :loading="albums.loadingMore.value"
                @load="albums.loadMore()"
            />
        </section>
        <section
            v-if="artistTiles.length > 0"
            data-part="section"
            data-item="artists"
            :aria-labelledby="artistsTitleId"
        >
            <h2 :id="artistsTitleId" data-part="section-title">
                {{ t('collection.artistsTitle') }}
            </h2>
            <TileList :tiles="artistTiles" :label="t('collection.artistsTitle')" />
            <LoadMore
                v-if="artists.hasMore.value"
                :loading="artists.loadingMore.value"
                @load="artists.loadMore()"
            />
        </section>
    </template>
</template>
