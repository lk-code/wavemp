<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { Track } from '@/api/generated/models'
import BaseContextMenu, { type MenuItem } from '@/components/base/BaseContextMenu.vue'
import AddToPlaylistDialog from '@/components/playlists/AddToPlaylistDialog.vue'
import { usePlayerStore } from '@/stores/player'
import { useSessionStore } from '@/stores/session'

/**
 * Context menu of a track: play it, play it next or add it to the queue (tracks with audio), add it
 * to a playlist, its details, its album and its artist; for admins also the metadata editor, which
 * opens on the page of the track. Play is passed to the list (event play), which plays its tracks
 * from this one. Lists can add entries of their own (extraItems, event extra), e.g. the playlist
 * page to move and remove entries.
 */
const props = withDefaults(
    defineProps<{
        extraItems?: readonly MenuItem[]
    }>(),
    { extraItems: () => [] },
)

const emit = defineEmits<{
    play: [track: Track]
    extra: [id: string, track: Track]
}>()

const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const player = usePlayerStore()
const menu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const track = ref<Track | null>(null)
const addingToPlaylist = ref(false)
const playlistTrackIds = computed(() => (track.value?.id ? [track.value.id] : []))

const items = computed<MenuItem[]>(() => {
    const current = track.value
    return [
        ...(current?.hasAudio
            ? ([
                  { id: 'play', label: t('trackMenu.play'), icon: 'play-now' },
                  { id: 'play-next', label: t('trackMenu.playNext'), icon: 'play-next' },
                  {
                      id: 'add-to-queue',
                      label: t('trackMenu.addToQueue'),
                      icon: 'add-to-queue',
                  },
              ] as const)
            : []),
        {
            id: 'add-to-playlist',
            label: t('trackMenu.addToPlaylist'),
            icon: 'add-to-playlist',
        },
        { id: 'separator-play', separator: true },
        { id: 'details', label: t('trackMenu.details'), icon: 'info' },
        ...(current?.album?.id
            ? [{ id: 'album', label: t('trackMenu.album'), icon: 'go-to-album' } as const]
            : []),
        ...(current?.artist?.id
            ? [{ id: 'artist', label: t('trackMenu.artist'), icon: 'go-to-artist' } as const]
            : []),
        ...(session.isAdmin
            ? [{ id: 'edit-metadata', label: t('trackMenu.editMetadata'), icon: 'edit' } as const]
            : []),
        ...(props.extraItems.length > 0
            ? [{ id: 'separator-extra', separator: true } as const, ...props.extraItems]
            : []),
    ]
})

/** Opens the menu of a track at a point in viewport coordinates. */
async function open(value: Track, x: number, y: number): Promise<void> {
    track.value = value
    await menu.value?.openAt(x, y)
}

function choose(id: string): void {
    const current = track.value
    if (!current?.id) {
        return
    }

    if (props.extraItems.some((item) => item.id === id)) {
        emit('extra', id, current)
    } else if (id === 'play') {
        emit('play', current)
    } else if (id === 'play-next') {
        player.playNext([current])
    } else if (id === 'add-to-queue') {
        player.addToQueue([current])
    } else if (id === 'add-to-playlist') {
        addingToPlaylist.value = true
    } else if (id === 'album' && current.album?.id) {
        void router.push({ name: 'library-album', params: { id: current.album.id } })
    } else if (id === 'artist' && current.artist?.id) {
        void router.push({ name: 'library-artist', params: { id: current.artist.id } })
    } else if (id === 'details') {
        void router.push({ name: 'library-track', params: { id: current.id } })
    } else if (id === 'edit-metadata') {
        void router.push({
            name: 'library-track',
            params: { id: current.id },
            query: { edit: 'metadata' },
        })
    }
}

defineExpose({ open })
</script>

<template>
    <BaseContextMenu ref="menu" :items="items" :label="t('trackMenu.label')" @select="choose" />
    <AddToPlaylistDialog v-model:open="addingToPlaylist" :track-ids="playlistTrackIds" />
</template>
