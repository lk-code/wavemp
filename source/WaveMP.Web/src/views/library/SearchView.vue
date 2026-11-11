<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import type { SearchResult } from '@/api/generated/models'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import TileList from '@/components/library/TileList.vue'
import TrackGroupList from '@/components/library/TrackGroupList.vue'
import { useLibraryTiles } from '@/composables/useLibraryTiles'
import { searchLibrary } from '@/library/api'
import type { TrackGroup } from '@/library/grouping'
import { queryText } from '@/library/search'

/**
 * Search over the whole library: the best artists, albums and songs for the text of the search
 * field (?q=). Each group links to its list filtered by the text.
 */
const { t } = useI18n()
const route = useRoute()
const tiles = useLibraryTiles()
const artistsTitleId = useId()
const albumsTitleId = useId()
const songsTitleId = useId()

/** Matches per group. */
const resultLimit = 10

const q = computed(() => queryText(route.query))
const result = ref<SearchResult | null>(null)
const status = ref<'idle' | 'loading' | 'ready' | 'error'>('idle')
let generation = 0

const artists = computed(() => (result.value?.artists?.items ?? []).map(tiles.artist))
const albums = computed(() => (result.value?.albums?.items ?? []).map(tiles.album))
const songs = computed<TrackGroup[]>(() => {
    const tracks = result.value?.tracks?.items ?? []
    return tracks.length === 0
        ? []
        : [
              {
                  id: 'songs',
                  title: t('search.songs'),
                  artist: null,
                  isCompilation: false,
                  year: null,
                  genre: null,
                  cover: null,
                  tracks,
              },
          ]
})
const empty = computed(
    () => artists.value.length === 0 && albums.value.length === 0 && songs.value.length === 0,
)

async function search(text: string): Promise<void> {
    const current = ++generation
    if (!text) {
        result.value = null
        status.value = 'idle'
        return
    }

    status.value = 'loading'
    try {
        const found = await searchLibrary(text, resultLimit)
        if (current === generation) {
            result.value = found ?? null
            status.value = 'ready'
        }
    } catch {
        if (current === generation) {
            status.value = 'error'
        }
    }
}

function showAll(section: string) {
    return { name: 'library-section', params: { section }, query: { q: q.value } }
}

watch(q, search, { immediate: true })
</script>

<template>
    <div
        data-part="view"
        data-view="library-search"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <div v-if="!q" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('search.promptTitle') }}</p>
            <p data-part="empty-state-text">{{ t('search.promptText') }}</p>
        </div>
        <div v-else-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('search.error') }}</p>
            <BaseButton :label="t('library.retry')" @click="search(q)" />
        </div>
        <div v-else-if="status === 'ready' && empty" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('search.noResultsTitle') }}</p>
            <p data-part="empty-state-text">{{ t('search.noResultsText', { query: q }) }}</p>
        </div>
        <template v-else-if="result">
            <section
                v-if="artists.length > 0"
                data-part="section"
                data-item="artists"
                :aria-labelledby="artistsTitleId"
            >
                <h2 :id="artistsTitleId" data-part="section-title">{{ t('search.artists') }}</h2>
                <TileList :tiles="artists" :label="t('search.artists')" />
                <BaseLink :to="showAll('artists')">{{ t('search.showAll') }}</BaseLink>
            </section>
            <section
                v-if="albums.length > 0"
                data-part="section"
                data-item="albums"
                :aria-labelledby="albumsTitleId"
            >
                <h2 :id="albumsTitleId" data-part="section-title">{{ t('search.albums') }}</h2>
                <TileList :tiles="albums" :label="t('search.albums')" />
                <BaseLink :to="showAll('albums')">{{ t('search.showAll') }}</BaseLink>
            </section>
            <section
                v-if="songs.length > 0"
                data-part="section"
                data-item="tracks"
                :aria-labelledby="songsTitleId"
            >
                <h2 :id="songsTitleId" data-part="section-title">{{ t('search.songs') }}</h2>
                <TrackGroupList
                    :groups="songs"
                    :label="t('search.songs')"
                    :show-group-info="false"
                />
                <BaseLink :to="showAll('songs')">{{ t('search.showAll') }}</BaseLink>
            </section>
        </template>
    </div>
</template>
