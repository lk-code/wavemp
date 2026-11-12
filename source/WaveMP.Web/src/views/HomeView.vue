<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Home, Track } from '@/api/generated/models'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import HomeShelf from '@/components/collection/HomeShelf.vue'
import { getHome } from '@/collection/api'
import { favoriteTile, homeRowLimit, playableTracks, withIds } from '@/collection/home'
import { useLibraryTiles } from '@/composables/useLibraryTiles'
import { usePlayerStore } from '@/stores/player'
import { useSessionStore } from '@/stores/session'

/**
 * The home page of the profile, the first page after the profile selection: its playlists and
 * rows of the songs played recently, the albums added recently, its favorites and its best rated
 * songs. Every row shows the first items and links to the whole list; rows without items are left
 * out.
 */
const { t } = useI18n()
const tiles = useLibraryTiles()
const player = usePlayerStore()
const session = useSessionStore()

const home = ref<Home | null>(null)
const status = ref<'loading' | 'ready' | 'error'>('loading')
let generation = 0

const recentlyPlayed = computed(() =>
    withIds((home.value?.recentlyPlayed?.items ?? []).map((item) => item.track)),
)
const topRated = computed(() => withIds(home.value?.topRated?.items ?? []))

const playlistTiles = computed(() =>
    withIds(home.value?.playlists?.items ?? []).map((playlist) => tiles.playlist(playlist)),
)
const recentlyPlayedTiles = computed(() => recentlyPlayed.value.map((track) => tiles.track(track)))
const recentlyAddedTiles = computed(() =>
    withIds(home.value?.recentlyAdded?.items ?? []).map((album) => tiles.album(album)),
)
const favoriteTiles = computed(() =>
    (home.value?.favorites?.items ?? []).flatMap((favorite) => favoriteTile(favorite, tiles)),
)
const topRatedTiles = computed(() => topRated.value.map((track) => tiles.track(track)))

const empty = computed(
    () =>
        status.value === 'ready' &&
        [
            playlistTiles.value,
            recentlyPlayedTiles.value,
            recentlyAddedTiles.value,
            favoriteTiles.value,
            topRatedTiles.value,
        ].every((row) => row.length === 0),
)

function play(tracks: readonly Track[]): void {
    const list = playableTracks(tracks)
    if (list.length > 0) {
        player.playTracks(list, 0)
    }
}

async function load(): Promise<void> {
    const current = ++generation
    status.value = 'loading'
    try {
        const loaded = await getHome(homeRowLimit)
        if (current === generation) {
            home.value = loaded ?? null
            status.value = 'ready'
        }
    } catch {
        if (current === generation) {
            status.value = 'error'
        }
    }
}

// The home page belongs to the profile of the session.
watch(() => session.token, load, { immediate: true })
</script>

<template>
    <div data-part="view" data-view="home" :aria-busy="status === 'loading' ? 'true' : undefined">
        <div v-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('home.loadError') }}</p>
            <BaseButton :label="t('library.retry')" @click="load" />
        </div>
        <div v-else-if="empty" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('home.emptyTitle') }}</p>
            <p data-part="empty-state-text">{{ t('home.emptyText') }}</p>
            <BaseLink :to="{ name: 'library' }">{{ t('library.toLibrary') }}</BaseLink>
        </div>
        <template v-else-if="home">
            <HomeShelf
                v-if="playlistTiles.length > 0"
                item="playlists"
                :title="t('home.playlists')"
                :tiles="playlistTiles"
                :to="{ name: 'playlists' }"
            />
            <HomeShelf
                v-if="recentlyPlayedTiles.length > 0"
                item="recently-played"
                :title="t('playlists.recentlyPlayed')"
                :tiles="recentlyPlayedTiles"
                :to="{ name: 'playlists-recently-played' }"
            >
                <template v-if="playableTracks(recentlyPlayed).length > 0" #actions>
                    <BaseButton
                        data-item="play"
                        icon="play-now"
                        :label="t('library.play')"
                        @click="play(recentlyPlayed)"
                    />
                </template>
            </HomeShelf>
            <HomeShelf
                v-if="recentlyAddedTiles.length > 0"
                item="recently-added"
                :title="t('library.recentlyAdded')"
                :tiles="recentlyAddedTiles"
                :to="{ name: 'library-section', params: { section: 'recently-added' } }"
            />
            <HomeShelf
                v-if="favoriteTiles.length > 0"
                item="favorites"
                :title="t('library.favorites')"
                :tiles="favoriteTiles"
                :to="{ name: 'library-section', params: { section: 'favorites' } }"
            />
            <HomeShelf
                v-if="topRatedTiles.length > 0"
                item="top-rated"
                :title="t('home.topRated')"
                :tiles="topRatedTiles"
                :to="{
                    name: 'library-section',
                    params: { section: 'songs' },
                    query: { sort: 'Rating', myMusic: 'true' },
                }"
            >
                <template v-if="playableTracks(topRated).length > 0" #actions>
                    <BaseButton
                        data-item="play"
                        icon="play-now"
                        :label="t('library.play')"
                        @click="play(topRated)"
                    />
                </template>
            </HomeShelf>
        </template>
    </div>
</template>
