<script lang="ts">
import type { Track } from '@/api/generated/models'

export interface PlaylistTrackRow {
    /** Id of the row: the entry of a playlist, or the track in lists without entries. */
    readonly id: string
    readonly track: Track
    /** Text of the detail column, e.g. when the track was played last. */
    readonly detail?: string
}
</script>

<script setup lang="ts">
import { computed, ref, type ComponentPublicInstance } from 'vue'
import { useI18n } from 'vue-i18n'
import type { MenuItem } from '@/components/base/BaseContextMenu.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseList, { type ListGroup, type ListRowBase } from '@/components/base/BaseList.vue'
import FavoriteButton from '@/components/collection/FavoriteButton.vue'
import RatingStars from '@/components/collection/RatingStars.vue'
import TrackMenu from '@/components/library/TrackMenu.vue'
import { useFormat } from '@/composables/useFormat'
import { useSortableList } from '@/composables/useSortableList'
import { artistLabelKey, durationSeconds, trackRowState } from '@/library/presentation'
import { usePlayerStore } from '@/stores/player'

/**
 * The tracks of a playlist (or of the list "Recently played") in their order, without grouping.
 * A double click, Enter or a tap plays the list from the row on. With sortable, each row has a
 * handle to drag it to another place (event move); the context menu adds the entries of the page
 * (extraItems, event extra), e.g. to move a row with the keyboard or to remove it. The columns
 * rating and favorite show the stars and the heart of the profile; a click changes them.
 */
interface Row extends ListRowBase {
    readonly entry: PlaylistTrackRow
    readonly index: number
}

const props = withDefaults(
    defineProps<{
        rows: readonly PlaylistTrackRow[]
        /** Accessible name of the list. */
        label: string
        sortable?: boolean
        /** Heading of the detail column; without it, the column is left out. */
        detailLabel?: string
        extraItems?: (row: PlaylistTrackRow, index: number) => readonly MenuItem[]
        /** data-state tokens of the list, e.g. "busy". */
        state?: string
    }>(),
    { sortable: false, detailLabel: undefined, extraItems: undefined, state: undefined },
)

const emit = defineEmits<{
    move: [from: number, to: number]
    extra: [id: string, row: PlaylistTrackRow, index: number]
}>()

/** Id of the selected row. */
const selected = defineModel<string | undefined>('selected', { default: undefined })

const { t } = useI18n()
const format = useFormat()
const player = usePlayerStore()
const list = ref<ComponentPublicInstance | null>(null)
const menu = ref<InstanceType<typeof TrackMenu> | null>(null)
const menuRow = ref<Row | null>(null)

const columns = computed(() => [
    ...(props.sortable ? [{ id: 'handle', label: t('list.columns.handle') }] : []),
    { id: 'number', label: t('list.columns.number') },
    { id: 'title', label: t('list.columns.title') },
    { id: 'duration', label: t('list.columns.duration') },
    { id: 'artist', label: t('list.columns.artist') },
    { id: 'album', label: t('list.columns.album') },
    ...(props.detailLabel ? [{ id: 'detail', label: props.detailLabel }] : []),
    { id: 'rating', label: t('list.columns.rating') },
    { id: 'favorite', label: t('list.columns.favorite') },
])

const playingId = computed(() => player.state.trackId)

const groups = computed<ListGroup<Row>[]>(() => [
    {
        id: 'tracks',
        title: props.label,
        rows: props.rows.map((entry, index) => ({
            id: entry.id,
            state: [
                ...trackRowState(entry.track),
                ...(entry.track.id && entry.track.id === playingId.value ? ['playing'] : []),
            ],
            entry,
            index,
        })),
    },
])

const rowsById = computed(() => new Map(groups.value[0]!.rows.map((row) => [row.id, row])))
const tracks = computed(() => props.rows.map((row) => row.track))

const rowsElement = computed(() => {
    const element: unknown = list.value?.$el
    return element instanceof HTMLElement
        ? element.querySelector<HTMLElement>('[data-part="list-group-rows"]')
        : null
})

useSortableList(rowsElement, {
    draggable: '[data-part="list-row"]',
    handle: '[data-part="drag-handle"]',
    disabled: computed(() => !props.sortable),
    onMove: (from, to) => emit('move', from, to),
})

const menuItems = computed(() =>
    menuRow.value && props.extraItems
        ? props.extraItems(menuRow.value.entry, menuRow.value.index)
        : [],
)

function artistOf(track: Track): string {
    const key = artistLabelKey(track.artist)
    return key ? t(key) : (track.artist?.name ?? '')
}

/** Plays the list from the row on; video-only tracks are left out. */
function playFrom(index: number): void {
    const track = tracks.value[index]
    if (track?.hasAudio) {
        player.playTracks(tracks.value, index)
    }
}

function activate(rowId: string): void {
    const row = rowsById.value.get(rowId)
    if (row) {
        playFrom(row.index)
    }
}

function openMenu(rowId: string, x: number, y: number): void {
    const row = rowsById.value.get(rowId)
    if (row) {
        menuRow.value = row
        void menu.value?.open(row.entry.track, x, y)
    }
}

function onPlay(): void {
    if (menuRow.value) {
        playFrom(menuRow.value.index)
    }
}

function onExtra(id: string): void {
    if (menuRow.value) {
        emit('extra', id, menuRow.value.entry, menuRow.value.index)
    }
}
</script>

<template>
    <BaseList
        ref="list"
        v-model:selected="selected"
        :columns="columns"
        :groups="groups"
        :label="label"
        :state="state"
        row-menu
        activate-on-tap
        @row-menu="openMenu"
        @row-activate="activate"
    >
        <template #cell="{ row, column }">
            <span
                v-if="column.id === 'handle'"
                data-part="drag-handle"
                :title="t('playlists.dragHandle')"
                @click.stop
            >
                <BaseIcon name="drag-handle" />
                <span data-part="label">{{ t('playlists.dragHandle') }}</span>
            </span>
            <template v-else-if="column.id === 'number'">
                {{ format.number(row.index + 1) }}
            </template>
            <template v-else-if="column.id === 'title'">
                <span data-part="label">{{ row.entry.track.title }}</span>
                <span
                    v-if="row.entry.track.hasVideo"
                    data-part="badge"
                    :data-state="trackRowState(row.entry.track).join(' ')"
                    :title="row.entry.track.hasAudio ? t('list.hasVideo') : t('list.videoOnly')"
                >
                    <BaseIcon name="has-video" />
                    <span data-part="label">{{
                        row.entry.track.hasAudio ? t('list.hasVideo') : t('list.videoOnly')
                    }}</span>
                </span>
            </template>
            <template v-else-if="column.id === 'duration'">
                {{
                    durationSeconds(row.entry.track.durationMilliseconds) === null
                        ? ''
                        : format.duration(durationSeconds(row.entry.track.durationMilliseconds)!)
                }}
            </template>
            <template v-else-if="column.id === 'artist'">
                {{ artistOf(row.entry.track) }}
            </template>
            <template v-else-if="column.id === 'album'">
                {{ row.entry.track.album?.title ?? '' }}
            </template>
            <template v-else-if="column.id === 'detail'">{{ row.entry.detail ?? '' }}</template>
            <RatingStars
                v-else-if="column.id === 'rating'"
                :track-id="row.entry.track.id ?? ''"
                :rating="row.entry.track.rating"
                :focusable="false"
            />
            <FavoriteButton
                v-else-if="column.id === 'favorite'"
                type="Track"
                :id="row.entry.track.id ?? ''"
                :favorite="row.entry.track.isFavorite"
                :focusable="false"
            />
        </template>
    </BaseList>
    <TrackMenu ref="menu" :extra-items="menuItems" @play="onPlay" @extra="onExtra" />
</template>
