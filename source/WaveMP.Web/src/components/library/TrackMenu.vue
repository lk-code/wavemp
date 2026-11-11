<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { Track } from '@/api/generated/models'
import BaseContextMenu, { type MenuItem } from '@/components/base/BaseContextMenu.vue'
import { usePlayerStore } from '@/stores/player'
import { useSessionStore } from '@/stores/session'

/**
 * Context menu of a track: play it, play it next or add it to the queue (tracks with audio), its
 * details, its album and its artist; for admins also the metadata editor, which opens on the page
 * of the track. Play is passed to the list (event play), which plays its tracks from this one.
 */
const emit = defineEmits<{
    play: [track: Track]
}>()

const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const player = usePlayerStore()
const menu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const track = ref<Track | null>(null)

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
                  { id: 'separator-play', separator: true },
              ] as const)
            : []),
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

    if (id === 'play') {
        emit('play', current)
    } else if (id === 'play-next') {
        player.playNext([current])
    } else if (id === 'add-to-queue') {
        player.addToQueue([current])
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
</template>
