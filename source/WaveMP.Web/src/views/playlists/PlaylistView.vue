<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import {
    PlaylistCoverKindObject,
    ProblemCodeObject,
    type Playlist,
    type PlaylistEntry,
} from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'
import { problemCodeOf, statusOf } from '@/api/problems'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import type { MenuItem } from '@/components/base/BaseContextMenu.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import PlaylistCover from '@/components/playlists/PlaylistCover.vue'
import PlaylistDeleteDialog from '@/components/playlists/PlaylistDeleteDialog.vue'
import PlaylistEditDialog from '@/components/playlists/PlaylistEditDialog.vue'
import PlaylistTrackList, {
    type PlaylistTrackRow,
} from '@/components/playlists/PlaylistTrackList.vue'
import { useFormat } from '@/composables/useFormat'
import { durationSeconds } from '@/library/presentation'
import {
    deletePlaylistCover,
    getPlaylist,
    listPlaylistEntries,
    movePlaylistEntry,
    removePlaylistEntry,
    uploadPlaylistCover,
} from '@/playlists/api'
import { coverUploadProblem, coverUploadTypes, moveItem } from '@/playlists/presentation'
import { usePageTitleStore } from '@/stores/pageTitle'
import { usePlayerStore } from '@/stores/player'
import { usePlaylistsStore } from '@/stores/playlists'

/**
 * A playlist of the current profile: its cover, details and entries. Entries are moved by drag and
 * drop (or with the context menu) and removed; the playlist is played, renamed, gets its own cover
 * or is deleted. Changes are shown at once and sent to the API; if that fails, the page loads
 * the playlist again.
 */
const props = defineProps<{
    id: string
}>()

const { t } = useI18n()
const format = useFormat()
const router = useRouter()
const pageTitle = usePageTitleStore()
const player = usePlayerStore()
const playlists = usePlaylistsStore()

const playlist = ref<Playlist | null>(null)
const entries = ref<PlaylistEntry[]>([])
const status = ref<'loading' | 'ready' | 'notFound' | 'error'>('loading')
const error = ref<string | null>(null)
const busy = ref(false)
const renaming = ref(false)
const deleting = ref(false)
const deleteError = ref<string | null>(null)
const coverInput = ref<HTMLInputElement | null>(null)
let generation = 0

const rows = computed<PlaylistTrackRow[]>(() =>
    entries.value
        .filter((entry) => entry.id && entry.track)
        .map((entry) => ({ id: entry.id!, track: entry.track! })),
)
const tracks = computed(() => rows.value.map((row) => row.track))
const hasAudio = computed(() => tracks.value.some((track) => track.hasAudio))
const duration = computed(() => durationSeconds(playlist.value?.durationMilliseconds))
const hasOwnCover = computed(() => playlist.value?.cover?.kind === PlaylistCoverKindObject.Custom)
const acceptedTypes = coverUploadTypes.join(',')

function setPlaylist(value: Playlist | undefined): void {
    if (value) {
        playlist.value = value
        playlists.apply(value)
        pageTitle.set(value.name ?? null)
    }
}

async function load(): Promise<void> {
    const current = ++generation
    status.value = 'loading'
    error.value = null
    pageTitle.clear()
    try {
        const [loaded, loadedEntries] = await Promise.all([
            getPlaylist(props.id),
            fetchAllPages((offset, limit) => listPlaylistEntries(props.id, offset, limit)),
        ])
        if (current !== generation) {
            return
        }

        entries.value = loadedEntries
        status.value = loaded ? 'ready' : 'notFound'
        setPlaylist(loaded)
    } catch (failure) {
        if (current === generation) {
            playlist.value = null
            status.value = statusOf(failure) === 404 ? 'notFound' : 'error'
            if (status.value === 'notFound') {
                playlists.forget(props.id)
            }
        }
    }
}

/** Loads the details again, e.g. the number of entries and the cover after a change. */
async function refresh(): Promise<void> {
    try {
        setPlaylist(await getPlaylist(props.id))
    } catch {
        // The entries are up to date; the details follow with the next change.
    }
}

/** Runs a change; on failure the page shows why and loads the playlist again. */
async function change(action: () => Promise<unknown>, failureKey: string): Promise<void> {
    busy.value = true
    error.value = null
    try {
        await action()
        await refresh()
    } catch (failure) {
        error.value =
            problemCodeOf(failure) === ProblemCodeObject.PlaylistNot_found
                ? t('playlists.errors.notFound')
                : t(failureKey)
        await load()
    } finally {
        busy.value = false
    }
}

function move(from: number, to: number): void {
    const entry = entries.value[from]
    if (!entry?.id || from === to || to < 0 || to >= entries.value.length) {
        return
    }

    entries.value = moveItem(entries.value, from, to).map((item, position) => ({
        ...item,
        position,
    }))
    const entryId = entry.id
    void change(() => movePlaylistEntry(props.id, entryId, to), 'playlists.errors.moveFailed')
}

function remove(index: number): void {
    const entry = entries.value[index]
    if (!entry?.id) {
        return
    }

    entries.value = entries.value.filter((_, position) => position !== index)
    const entryId = entry.id
    void change(() => removePlaylistEntry(props.id, entryId), 'playlists.errors.removeFailed')
}

function extraItems(_row: PlaylistTrackRow, index: number): MenuItem[] {
    return [
        {
            id: 'move-up',
            label: t('playlists.moveUp'),
            icon: 'move-up',
            disabled: index === 0 || busy.value,
        },
        {
            id: 'move-down',
            label: t('playlists.moveDown'),
            icon: 'move-down',
            disabled: index === rows.value.length - 1 || busy.value,
        },
        {
            id: 'remove-from-playlist',
            label: t('playlists.removeEntry'),
            icon: 'remove-from-playlist',
            disabled: busy.value,
        },
    ]
}

function onExtra(id: string, _row: PlaylistTrackRow, index: number): void {
    if (id === 'move-up') {
        move(index, index - 1)
    } else if (id === 'move-down') {
        move(index, index + 1)
    } else if (id === 'remove-from-playlist') {
        remove(index)
    }
}

function play(shuffle: boolean): void {
    player.playTracks(tracks.value, shuffle ? -1 : 0, { shuffle })
}

function chooseCover(): void {
    coverInput.value?.click()
}

function coverFailure(failure: unknown): string {
    switch (problemCodeOf(failure)) {
        case ProblemCodeObject.CoverUnsupported_format:
            return t('playlists.errors.coverType')
        case ProblemCodeObject.CoverToo_large:
            return t('playlists.errors.coverSize')
        case ProblemCodeObject.CoverUnreadable:
            return t('playlists.errors.coverUnreadable')
        case ProblemCodeObject.PlaylistNot_found:
            return t('playlists.errors.notFound')
        default:
            return t('playlists.errors.coverFailed')
    }
}

async function uploadCover(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement
    const file = input.files?.[0]
    input.value = ''
    if (!file) {
        return
    }

    const problem = coverUploadProblem(file)
    if (problem) {
        error.value = t(
            problem === 'type' ? 'playlists.errors.coverType' : 'playlists.errors.coverSize',
        )
        return
    }

    busy.value = true
    error.value = null
    try {
        setPlaylist(await uploadPlaylistCover(props.id, await file.arrayBuffer(), file.type))
    } catch (failure) {
        error.value = coverFailure(failure)
    } finally {
        busy.value = false
    }
}

function removeCover(): void {
    void change(() => deletePlaylistCover(props.id), 'playlists.errors.coverFailed')
}

function renamed(value: Playlist): void {
    setPlaylist(value)
}

async function confirmDelete(): Promise<void> {
    busy.value = true
    deleteError.value = null
    try {
        await playlists.remove(props.id)
        deleting.value = false
        void router.push({ name: 'playlists' })
    } catch (failure) {
        if (statusOf(failure) === 404) {
            playlists.forget(props.id)
            deleting.value = false
            void router.push({ name: 'playlists' })
        } else {
            deleteError.value = t('playlists.errors.deleteFailed')
        }
    } finally {
        busy.value = false
    }
}

watch(() => props.id, load, { immediate: true })
onBeforeUnmount(() => pageTitle.clear())
</script>

<template>
    <div
        data-part="view"
        data-view="playlist"
        :aria-busy="status === 'loading' || busy ? 'true' : undefined"
    >
        <div v-if="status === 'notFound'" data-part="empty-state">
            <p data-part="empty-state-text">{{ t('playlists.errors.notFound') }}</p>
            <BaseLink :to="{ name: 'playlists' }">{{ t('playlists.toPlaylists') }}</BaseLink>
        </div>
        <div v-else-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('playlists.errors.loadFailed') }}</p>
            <BaseButton :label="t('library.retry')" @click="load" />
        </div>
        <template v-else-if="playlist">
            <DetailHeader :title="playlist.name ?? ''">
                <template #media>
                    <PlaylistCover :id="playlist.id" :cover="playlist.cover" />
                </template>
                <MetaItem field="tracks" :label="t('list.songs')">
                    {{ format.number(playlist.trackCount ?? 0) }}
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
                    <BaseButton
                        data-item="rename"
                        icon="edit"
                        :label="t('playlists.rename')"
                        :disabled="busy"
                        @click="renaming = true"
                    />
                    <BaseButton
                        data-item="cover"
                        icon="change-cover"
                        :label="t('playlists.changeCover')"
                        :disabled="busy"
                        @click="chooseCover"
                    />
                    <BaseButton
                        v-if="hasOwnCover"
                        data-item="remove-cover"
                        icon="delete"
                        :label="t('playlists.removeCover')"
                        :disabled="busy"
                        @click="removeCover"
                    />
                    <BaseButton
                        data-item="delete"
                        icon="delete"
                        :label="t('playlists.delete')"
                        :disabled="busy"
                        @click="deleting = true"
                    />
                    <input
                        ref="coverInput"
                        type="file"
                        :accept="acceptedTypes"
                        hidden
                        @change="uploadCover"
                    />
                </template>
            </DetailHeader>
            <BaseAlert v-if="error" tone="error" :message="error" />
            <div v-if="rows.length === 0" data-part="empty-state">
                <p data-part="empty-state-title">{{ t('playlists.noEntriesTitle') }}</p>
                <p data-part="empty-state-text">{{ t('playlists.noEntriesText') }}</p>
            </div>
            <PlaylistTrackList
                v-else
                :rows="rows"
                :label="playlist.name ?? ''"
                sortable
                :extra-items="extraItems"
                :state="busy ? 'busy' : undefined"
                @move="move"
                @extra="onExtra"
            />
            <PlaylistEditDialog v-model:open="renaming" :playlist="playlist" @saved="renamed" />
            <PlaylistDeleteDialog
                v-model:open="deleting"
                :playlist="playlist"
                :busy="busy"
                :error="deleteError"
                @confirm="confirmDelete"
            />
        </template>
    </div>
</template>
