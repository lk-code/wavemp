<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseList, { type ListGroup, type ListRowBase } from '@/components/base/BaseList.vue'
import { useFormat } from '@/composables/useFormat'
import { placeholderAlbums } from '@/placeholders/library'

/** Songs grouped by album. Shows placeholder content until the library API exists (step 6). */
interface TrackRow extends ListRowBase {
    readonly number: number
    readonly title: string
    readonly durationSeconds: number
    readonly artist: string
    readonly hasVideo: boolean
}

interface AlbumInfo {
    readonly title: string
    readonly artist: string
    readonly genre: string
    readonly year: number
}

const { t } = useI18n()
const format = useFormat()

const selected = ref<string | undefined>(undefined)

const columns = computed(() => [
    { id: 'number', label: t('list.columns.number') },
    { id: 'title', label: t('list.columns.title') },
    { id: 'duration', label: t('list.columns.duration') },
    { id: 'artist', label: t('list.columns.artist') },
])

const albums = computed(() =>
    placeholderAlbums.map((album) => {
        const info: AlbumInfo = {
            title: t('placeholders.album', { number: album.number }),
            artist: t('placeholders.artist', { number: album.artistNumber }),
            genre: t('placeholders.genre', { number: album.genreNumber }),
            year: album.year,
        }
        const group: ListGroup<TrackRow> = {
            id: `album-${album.number}`,
            title: info.title,
            rows: album.tracks.map((track) => ({
                id: `track-${track.titleNumber}`,
                state: track.hasVideo ? ['has-video'] : [],
                number: track.number,
                title: t('placeholders.track', { number: track.titleNumber }),
                durationSeconds: track.durationSeconds,
                artist: info.artist,
                hasVideo: track.hasVideo,
            })),
        }
        return { group, info }
    }),
)

const groups = computed(() => albums.value.map((album) => album.group))
const infos = computed(() => new Map(albums.value.map((album) => [album.group.id, album.info])))
</script>

<template>
    <BaseList
        v-model:selected="selected"
        :columns="columns"
        :groups="groups"
        :label="t('library.songs')"
        state="placeholder"
    >
        <template #group-info="{ group }">
            <BaseCover />
            <div data-part="list-group-meta" role="gridcell" :aria-colspan="columns.length">
                <span data-part="meta-item" data-field="album">
                    <span data-part="label">{{ t('list.album') }}</span>
                    <span data-part="meta-value">{{ infos.get(group.id)?.title }}</span>
                </span>
                <span data-part="meta-item" data-field="artist">
                    <span data-part="label">{{ t('list.artist') }}</span>
                    <span data-part="meta-value">{{ infos.get(group.id)?.artist }}</span>
                </span>
                <span data-part="meta-item" data-field="genre">
                    <span data-part="label">{{ t('list.genre') }}</span>
                    <span data-part="meta-value">{{ infos.get(group.id)?.genre }}</span>
                </span>
                <span data-part="meta-item" data-field="year">
                    <span data-part="label">{{ t('list.year') }}</span>
                    <span data-part="meta-value">{{
                        format.year(infos.get(group.id)?.year ?? 0)
                    }}</span>
                </span>
            </div>
        </template>
        <template #cell="{ row, column }">
            <template v-if="column.id === 'number'">{{ format.number(row.number) }}</template>
            <template v-else-if="column.id === 'title'">
                <span data-part="label">{{ row.title }}</span>
                <span
                    v-if="row.hasVideo"
                    data-part="badge"
                    data-state="has-video"
                    :title="t('list.hasVideo')"
                >
                    <BaseIcon name="has-video" />
                    <span data-part="label">{{ t('list.hasVideo') }}</span>
                </span>
            </template>
            <template v-else-if="column.id === 'duration'">
                {{ format.duration(row.durationSeconds) }}
            </template>
            <template v-else>{{ row.artist }}</template>
        </template>
    </BaseList>
</template>
