<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Album, Track } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'
import { statusOf } from '@/api/problems'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import FavoriteButton from '@/components/collection/FavoriteButton.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import TrackGroupList from '@/components/library/TrackGroupList.vue'
import { useFormat } from '@/composables/useFormat'
import { getAlbum, listAlbumTracks } from '@/library/api'
import { groupTracksByDisc } from '@/library/grouping'
import { artistLabelKey, durationSeconds } from '@/library/presentation'
import { usePageTitleStore } from '@/stores/pageTitle'
import { usePlayerStore } from '@/stores/player'

/**
 * An album with its cover, details and all its tracks in the order of disc and track number, and the
 * heart that marks it as favorite.
 */
const props = defineProps<{
    id: string
}>()

const { t } = useI18n()
const format = useFormat()
const pageTitle = usePageTitleStore()
const player = usePlayerStore()

const album = ref<Album | null>(null)
const tracks = ref<Track[]>([])
const status = ref<'loading' | 'ready' | 'notFound' | 'error'>('loading')
let generation = 0

const groups = computed(() =>
    album.value ? groupTracksByDisc(tracks.value, album.value.title ?? '') : [],
)
const artistKey = computed(() =>
    artistLabelKey(album.value?.artist, album.value?.isCompilation ?? false),
)
const duration = computed(() => durationSeconds(album.value?.durationMilliseconds))
/** Tracks with audio in the order of the page (discs, then track numbers). */
const playable = computed(() =>
    groups.value.flatMap((group) => group.tracks).filter((track) => track.hasAudio),
)
const hasAudio = computed(() => playable.value.length > 0)

function play(shuffle: boolean): void {
    player.playTracks(playable.value, shuffle ? -1 : 0, { shuffle })
}

async function load(): Promise<void> {
    const current = ++generation
    status.value = 'loading'
    pageTitle.clear()
    try {
        const [loaded, loadedTracks] = await Promise.all([
            getAlbum(props.id),
            fetchAllPages((offset, limit) => listAlbumTracks(props.id, { offset, limit })),
        ])
        if (current !== generation) {
            return
        }

        album.value = loaded ?? null
        tracks.value = loadedTracks
        status.value = loaded ? 'ready' : 'notFound'
        pageTitle.set(loaded?.title ?? null)
    } catch (error) {
        if (current === generation) {
            album.value = null
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
        data-view="library-album"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <div v-if="status === 'notFound'" data-part="empty-state">
            <p data-part="empty-state-text">{{ t('library.albumNotFound') }}</p>
            <BaseLink :to="{ name: 'library-section', params: { section: 'albums' } }">
                {{ t('library.toLibrary') }}
            </BaseLink>
        </div>
        <div v-else-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('library.loadError') }}</p>
            <BaseButton :label="t('library.retry')" @click="load" />
        </div>
        <template v-else-if="album">
            <DetailHeader :title="album.title ?? ''">
                <template #media>
                    <LibraryCover
                        kind="album"
                        :id="album.id"
                        :available="album.hasCover ?? false"
                    />
                </template>
                <MetaItem field="artist" :label="t('list.artist')">
                    <BaseLink
                        v-if="album.artist?.id"
                        :to="{ name: 'library-artist', params: { id: album.artist.id } }"
                    >
                        {{ album.artist.name }}
                    </BaseLink>
                    <template v-else>{{ t(artistKey ?? 'library.unknownArtist') }}</template>
                </MetaItem>
                <MetaItem v-if="album.year" field="year" :label="t('list.year')">
                    <BaseLink :to="{ name: 'library-year', params: { year: String(album.year) } }">
                        {{ format.year(album.year) }}
                    </BaseLink>
                </MetaItem>
                <MetaItem v-if="album.genre" field="genre" :label="t('list.genre')">
                    <BaseLink :to="{ name: 'library-genre', params: { genre: album.genre } }">
                        {{ album.genre }}
                    </BaseLink>
                </MetaItem>
                <MetaItem field="tracks" :label="t('list.songs')">
                    {{ format.number(album.trackCount ?? 0) }}
                </MetaItem>
                <MetaItem v-if="duration !== null" field="duration" :label="t('list.duration')">
                    {{ format.duration(duration) }}
                </MetaItem>
                <template #actions>
                    <BaseButton
                        v-if="hasAudio"
                        data-item="play"
                        variant="primary"
                        icon="play-now"
                        :label="t('library.play')"
                        @click="play(false)"
                    />
                    <BaseButton
                        v-if="hasAudio"
                        data-item="shuffle"
                        icon="shuffle-play"
                        :label="t('library.shufflePlay')"
                        @click="play(true)"
                    />
                    <FavoriteButton
                        data-item="favorite"
                        type="Album"
                        :id="album.id ?? id"
                        :favorite="album.isFavorite"
                    />
                </template>
            </DetailHeader>
            <TrackGroupList :groups="groups" :label="album.title ?? ''" :show-group-info="false" />
        </template>
    </div>
</template>
