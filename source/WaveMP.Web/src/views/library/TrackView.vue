<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Track } from '@/api/generated/models'
import { statusOf } from '@/api/problems'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import { useFormat } from '@/composables/useFormat'
import { getTrack } from '@/library/api'
import { artistLabelKey, durationSeconds, mediaKey } from '@/library/presentation'
import { usePageTitleStore } from '@/stores/pageTitle'

/** The details of a track: its cover, artist, album, numbers, year, genre, length and media. */
const props = defineProps<{
    id: string
}>()

const { t } = useI18n()
const format = useFormat()
const pageTitle = usePageTitleStore()

const track = ref<Track | null>(null)
const status = ref<'loading' | 'ready' | 'notFound' | 'error'>('loading')
let generation = 0

const duration = computed(() => durationSeconds(track.value?.durationMilliseconds))

async function load(): Promise<void> {
    const current = ++generation
    status.value = 'loading'
    pageTitle.clear()
    try {
        const loaded = await getTrack(props.id)
        if (current !== generation) {
            return
        }

        track.value = loaded ?? null
        status.value = loaded ? 'ready' : 'notFound'
        pageTitle.set(loaded?.title ?? null)
    } catch (error) {
        if (current === generation) {
            track.value = null
            status.value = statusOf(error) === 404 ? 'notFound' : 'error'
        }
    }
}

watch(() => props.id, load, { immediate: true })
onBeforeUnmount(() => pageTitle.clear())
</script>

<template>
    <div
        data-part="view"
        data-view="library-track"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <div v-if="status === 'notFound'" data-part="empty-state">
            <p data-part="empty-state-text">{{ t('library.trackNotFound') }}</p>
            <BaseLink :to="{ name: 'library-section', params: { section: 'songs' } }">
                {{ t('library.toLibrary') }}
            </BaseLink>
        </div>
        <div v-else-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('library.loadError') }}</p>
            <BaseButton :label="t('library.retry')" @click="load" />
        </div>
        <template v-else-if="track">
            <DetailHeader :title="track.title ?? ''">
                <template #media>
                    <LibraryCover
                        kind="track"
                        :id="track.id"
                        :available="track.hasCover ?? false"
                    />
                </template>
                <MetaItem field="artist" :label="t('list.artist')">
                    <BaseLink
                        v-if="track.artist?.id"
                        :to="{ name: 'library-artist', params: { id: track.artist.id } }"
                    >
                        {{ track.artist.name }}
                    </BaseLink>
                    <template v-else>{{
                        t(artistLabelKey(track.artist) ?? 'library.unknownArtist')
                    }}</template>
                </MetaItem>
                <MetaItem field="album" :label="t('list.album')">
                    <BaseLink
                        v-if="track.album?.id"
                        :to="{ name: 'library-album', params: { id: track.album.id } }"
                    >
                        {{ track.album.title }}
                    </BaseLink>
                    <template v-else>{{ t('library.unknownAlbum') }}</template>
                </MetaItem>
                <MetaItem v-if="track.trackNumber" field="number" :label="t('list.number')">
                    {{ format.number(track.trackNumber) }}
                </MetaItem>
                <MetaItem v-if="track.discNumber" field="disc" :label="t('list.disc')">
                    {{ format.number(track.discNumber) }}
                </MetaItem>
                <MetaItem v-if="track.year" field="year" :label="t('list.year')">
                    <BaseLink :to="{ name: 'library-year', params: { year: String(track.year) } }">
                        {{ format.year(track.year) }}
                    </BaseLink>
                </MetaItem>
                <MetaItem v-if="track.genre" field="genre" :label="t('list.genre')">
                    <BaseLink :to="{ name: 'library-genre', params: { genre: track.genre } }">
                        {{ track.genre }}
                    </BaseLink>
                </MetaItem>
                <MetaItem v-if="duration !== null" field="duration" :label="t('list.duration')">
                    {{ format.duration(duration) }}
                </MetaItem>
                <MetaItem field="media" :label="t('list.media')">
                    {{ t(mediaKey({ hasAudio: track.hasAudio, hasVideo: track.hasVideo })) }}
                </MetaItem>
            </DetailHeader>
        </template>
    </div>
</template>
