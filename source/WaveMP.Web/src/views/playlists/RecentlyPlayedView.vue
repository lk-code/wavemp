<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { RecentlyPlayedTrack } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'
import BaseButton from '@/components/base/BaseButton.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import PlaylistTrackList, {
    type PlaylistTrackRow,
} from '@/components/playlists/PlaylistTrackList.vue'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import { useFormat } from '@/composables/useFormat'
import { listRecentlyPlayed } from '@/playlists/api'
import { usePlayerStore } from '@/stores/player'

/**
 * The automatic list "Recently played": the tracks the profile played last, newest first, each
 * once. It follows the play history and cannot be edited.
 */
const { t } = useI18n()
const format = useFormat()
const player = usePlayerStore()

const items = ref<RecentlyPlayedTrack[]>([])
const status = ref<'loading' | 'ready' | 'error'>('loading')

const rows = computed<PlaylistTrackRow[]>(() =>
    items.value
        .filter((item) => item.track?.id)
        .map((item) => ({
            id: item.track!.id!,
            track: item.track!,
            detail: item.lastPlayedAt
                ? format.date(item.lastPlayedAt, { dateStyle: 'medium', timeStyle: 'short' })
                : '',
        })),
)
const tracks = computed(() => rows.value.map((row) => row.track))
const hasAudio = computed(() => tracks.value.some((track) => track.hasAudio))

async function load(): Promise<void> {
    status.value = 'loading'
    try {
        items.value = await fetchAllPages((offset, limit) => listRecentlyPlayed(offset, limit))
        status.value = 'ready'
    } catch {
        status.value = 'error'
    }
}

function play(shuffle: boolean): void {
    player.playTracks(tracks.value, shuffle ? -1 : 0, { shuffle })
}

onMounted(load)
</script>

<template>
    <div
        data-part="view"
        data-view="recently-played"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <div v-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('playlists.errors.loadFailed') }}</p>
            <BaseButton :label="t('library.retry')" @click="load" />
        </div>
        <template v-else>
            <DetailHeader :title="t('playlists.recentlyPlayed')">
                <template #media>
                    <BaseCover :placeholder="false">
                        <BaseIcon name="recently-played" />
                    </BaseCover>
                </template>
                <MetaItem field="tracks" :label="t('list.songs')">
                    {{ format.number(rows.length) }}
                </MetaItem>
                <template v-if="hasAudio" #actions>
                    <BaseButton
                        data-item="play"
                        variant="primary"
                        icon="play-now"
                        :label="t('library.play')"
                        @click="play(false)"
                    />
                    <BaseButton
                        data-item="shuffle"
                        icon="shuffle-play"
                        :label="t('library.shufflePlay')"
                        @click="play(true)"
                    />
                </template>
            </DetailHeader>
            <div v-if="status === 'ready' && rows.length === 0" data-part="empty-state">
                <p data-part="empty-state-title">{{ t('playlists.recentlyPlayedEmptyTitle') }}</p>
                <p data-part="empty-state-text">{{ t('playlists.recentlyPlayedEmptyText') }}</p>
            </div>
            <PlaylistTrackList
                v-else-if="rows.length > 0"
                :rows="rows"
                :label="t('playlists.recentlyPlayed')"
                :detail-label="t('playlists.lastPlayed')"
            />
        </template>
    </div>
</template>
