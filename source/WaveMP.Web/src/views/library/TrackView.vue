<script setup lang="ts">
import { computed, onBeforeUnmount, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import type { MediaFile, Track } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'
import { statusOf } from '@/api/problems'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import FavoriteButton from '@/components/collection/FavoriteButton.vue'
import RatingStars from '@/components/collection/RatingStars.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import TrackFileList from '@/components/library/TrackFileList.vue'
import TrackMetadataDialog from '@/components/library/TrackMetadataDialog.vue'
import { useFormat } from '@/composables/useFormat'
import { getTrack, listTrackFiles } from '@/library/api'
import { artistLabelKey, durationSeconds, mediaKey } from '@/library/presentation'
import { usePageTitleStore } from '@/stores/pageTitle'
import { usePlayerStore } from '@/stores/player'
import { useSessionStore } from '@/stores/session'

/**
 * The details of a track: its cover, artist, album, numbers, year, genre, length and media, its
 * rating and the heart that marks it as favorite. Admins also see all its files and edit its
 * metadata; the query edit=metadata opens the editor at once (context menu of a track).
 */
const props = defineProps<{
    id: string
}>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const format = useFormat()
const pageTitle = usePageTitleStore()
const session = useSessionStore()
const player = usePlayerStore()
const filesTitleId = useId()

const track = ref<Track | null>(null)
const status = ref<'loading' | 'ready' | 'notFound' | 'error'>('loading')
const files = ref<MediaFile[]>([])
const filesStatus = ref<'idle' | 'loading' | 'ready' | 'error'>('idle')
const editing = ref(false)
let generation = 0
let editRequestHandled = false

const duration = computed(() => durationSeconds(track.value?.durationMilliseconds))
const editRequested = computed(() => route.query.edit === 'metadata')

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

async function reload(): Promise<void> {
    await load()
    if (status.value === 'ready' && session.isAdmin) {
        await loadFiles()
    }
}

async function loadFiles(): Promise<void> {
    const current = generation
    filesStatus.value = 'loading'
    try {
        const loaded = await fetchAllPages((offset, limit) =>
            listTrackFiles(props.id, { offset, limit }),
        )
        if (current === generation) {
            files.value = loaded
            filesStatus.value = 'ready'
        }
    } catch {
        if (current === generation) {
            filesStatus.value = 'error'
        }
    }
}

watch(
    () => props.id,
    () => {
        files.value = []
        filesStatus.value = 'idle'
        editRequestHandled = false
        void reload()
    },
    { immediate: true },
)

// Opens the editor once per request, as soon as the track is loaded.
watch(
    [editRequested, status],
    ([requested, current]) => {
        if (!requested) {
            editRequestHandled = false
        } else if (!editRequestHandled && current === 'ready' && session.isAdmin) {
            editRequestHandled = true
            editing.value = true
        }
    },
    { immediate: true },
)

// Closing the editor removes the request from the address, so reloading the page does not open it.
watch(editing, (value) => {
    if (!value && editRequested.value) {
        void router.replace({ query: { ...route.query, edit: undefined } })
    }
})

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
            <BaseButton :label="t('library.retry')" @click="reload" />
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
                <MetaItem field="rating" :label="t('collection.rating')">
                    <RatingStars :track-id="track.id ?? id" :rating="track.rating" />
                </MetaItem>
                <template #actions>
                    <BaseButton
                        v-if="track.hasAudio"
                        data-item="play"
                        variant="primary"
                        icon="play-now"
                        :label="t('library.play')"
                        @click="player.playTracks([track], 0)"
                    />
                    <FavoriteButton
                        data-item="favorite"
                        type="Track"
                        :id="track.id ?? id"
                        :favorite="track.isFavorite"
                    />
                    <BaseButton
                        v-if="session.isAdmin"
                        data-item="edit-metadata"
                        icon="edit"
                        :label="t('trackMetadata.edit')"
                        @click="editing = true"
                    />
                </template>
            </DetailHeader>
            <section
                v-if="session.isAdmin"
                data-part="section"
                data-item="files"
                :aria-labelledby="filesTitleId"
            >
                <h2 :id="filesTitleId" data-part="section-title">{{ t('track.filesTitle') }}</h2>
                <div v-if="filesStatus === 'error'" data-part="empty-state">
                    <p data-part="empty-state-title">{{ t('track.filesError') }}</p>
                    <BaseButton :label="t('library.retry')" @click="loadFiles" />
                </div>
                <TrackFileList
                    v-else-if="files.length > 0"
                    :files="files"
                    :label="t('files.label')"
                />
            </section>
            <TrackMetadataDialog
                v-if="session.isAdmin"
                v-model:open="editing"
                :track-id="id"
                @saved="reload"
            />
        </template>
    </div>
</template>
