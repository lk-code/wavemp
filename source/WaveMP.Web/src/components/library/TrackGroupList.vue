<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Track } from '@/api/generated/models'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import BaseList, { type ListGroup, type ListRowBase } from '@/components/base/BaseList.vue'
import LibraryCover from './LibraryCover.vue'
import MetaItem from './MetaItem.vue'
import TrackMenu from './TrackMenu.vue'
import { useFormat } from '@/composables/useFormat'
import type { TrackGroup } from '@/library/grouping'
import { artistLabelKey, durationSeconds, trackRowState } from '@/library/presentation'

/**
 * Tracks grouped by album (or by disc on the album page), with cover and details of each group.
 * Details link to the pages of album, artist, genre and year; the context menu of a row leads to
 * the track, its album and its artist.
 */
interface TrackRow extends ListRowBase {
    readonly track: Track
}

const props = withDefaults(
    defineProps<{
        groups: readonly TrackGroup[]
        /** Accessible name of the list. */
        label: string
        /** Shows cover and details of each group; off where the page shows them already. */
        showGroupInfo?: boolean
        /** data-state tokens of the list, e.g. "placeholder". */
        state?: string
    }>(),
    { showGroupInfo: true, state: undefined },
)

/** Id of the selected track. */
const selected = defineModel<string | undefined>('selected', { default: undefined })

const { t } = useI18n()
const format = useFormat()
const menu = ref<InstanceType<typeof TrackMenu> | null>(null)

const columns = computed(() => [
    { id: 'number', label: t('list.columns.number') },
    { id: 'title', label: t('list.columns.title') },
    { id: 'duration', label: t('list.columns.duration') },
    { id: 'artist', label: t('list.columns.artist') },
])

function groupTitle(group: TrackGroup): string {
    if (group.disc !== undefined) {
        return t('library.disc', { number: format.number(group.disc) })
    }

    return group.title ?? t('library.unknownAlbum')
}

const listGroups = computed<ListGroup<TrackRow>[]>(() =>
    props.groups.map((group) => ({
        id: group.id,
        title: groupTitle(group),
        rows: group.tracks.map((track) => ({
            id: track.id ?? '',
            state: trackRowState(track),
            track,
        })),
    })),
)

const groupsById = computed(() => new Map(props.groups.map((group) => [group.id, group])))

function infoOf(group: ListGroup<TrackRow>): TrackGroup | undefined {
    return groupsById.value.get(group.id)
}

function artistName(group: TrackGroup): string {
    const key = artistLabelKey(group.artist, group.isCompilation)
    return key ? t(key) : (group.artist?.name ?? '')
}

function trackArtist(track: Track): string {
    const key = artistLabelKey(track.artist)
    return key ? t(key) : (track.artist?.name ?? '')
}

function openMenu(rowId: string, x: number, y: number): void {
    const track = props.groups.flatMap((group) => group.tracks).find((item) => item.id === rowId)
    if (track) {
        void menu.value?.open(track, x, y)
    }
}
</script>

<template>
    <BaseList
        v-model:selected="selected"
        :columns="columns"
        :groups="listGroups"
        :label="label"
        :state="state"
        row-menu
        @row-menu="openMenu"
    >
        <template v-if="showGroupInfo" #group-info="{ group }">
            <template v-if="infoOf(group)">
                <LibraryCover
                    v-if="infoOf(group)?.cover"
                    :kind="infoOf(group)!.cover!.kind"
                    :id="infoOf(group)!.cover!.id"
                    :available="infoOf(group)!.cover!.available"
                />
                <BaseCover v-else />
                <div data-part="list-group-meta" role="gridcell" :aria-colspan="columns.length">
                    <MetaItem v-if="infoOf(group)!.albumId" field="album" :label="t('list.album')">
                        <BaseLink
                            :to="{ name: 'library-album', params: { id: infoOf(group)!.albumId } }"
                        >
                            {{ group.title }}
                        </BaseLink>
                    </MetaItem>
                    <MetaItem field="artist" :label="t('list.artist')">
                        <BaseLink
                            v-if="infoOf(group)!.artist?.id"
                            :to="{
                                name: 'library-artist',
                                params: { id: infoOf(group)!.artist!.id },
                            }"
                        >
                            {{ artistName(infoOf(group)!) }}
                        </BaseLink>
                        <template v-else>{{ artistName(infoOf(group)!) }}</template>
                    </MetaItem>
                    <MetaItem v-if="infoOf(group)!.genre" field="genre" :label="t('list.genre')">
                        <BaseLink
                            :to="{ name: 'library-genre', params: { genre: infoOf(group)!.genre } }"
                        >
                            {{ infoOf(group)!.genre }}
                        </BaseLink>
                    </MetaItem>
                    <MetaItem v-if="infoOf(group)!.year" field="year" :label="t('list.year')">
                        <BaseLink
                            :to="{
                                name: 'library-year',
                                params: { year: String(infoOf(group)!.year) },
                            }"
                        >
                            {{ format.year(infoOf(group)!.year!) }}
                        </BaseLink>
                    </MetaItem>
                </div>
            </template>
        </template>
        <template #cell="{ row, column }">
            <template v-if="column.id === 'number'">
                {{ row.track.trackNumber ? format.number(row.track.trackNumber) : '' }}
            </template>
            <template v-else-if="column.id === 'title'">
                <span data-part="label">{{ row.track.title }}</span>
                <span
                    v-if="row.track.hasVideo"
                    data-part="badge"
                    :data-state="trackRowState(row.track).join(' ')"
                    :title="row.track.hasAudio ? t('list.hasVideo') : t('list.videoOnly')"
                >
                    <BaseIcon name="has-video" />
                    <span data-part="label">{{
                        row.track.hasAudio ? t('list.hasVideo') : t('list.videoOnly')
                    }}</span>
                </span>
            </template>
            <template v-else-if="column.id === 'duration'">
                {{
                    durationSeconds(row.track.durationMilliseconds) === null
                        ? ''
                        : format.duration(durationSeconds(row.track.durationMilliseconds)!)
                }}
            </template>
            <template v-else>{{ trackArtist(row.track) }}</template>
        </template>
    </BaseList>
    <TrackMenu ref="menu" />
</template>
