<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ProblemCodeObject, type Playlist } from '@/api/generated/models'
import { problemCodeOf } from '@/api/problems'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'
import PlaylistCover from './PlaylistCover.vue'
import PlaylistEditDialog from './PlaylistEditDialog.vue'
import { useFormat } from '@/composables/useFormat'
import { usePlaylistsStore } from '@/stores/playlists'

/**
 * Adds tracks to the end of a playlist of the current profile, or creates a new playlist with them.
 * A track can be added to a playlist that contains it already.
 */
const props = defineProps<{
    trackIds: readonly string[]
}>()

const emit = defineEmits<{
    added: [playlist: Playlist]
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
const format = useFormat()
const playlists = usePlaylistsStore()

const busy = ref<string | null>(null)
const error = ref<string | null>(null)
const creating = ref(false)

const loading = computed(() => playlists.status === 'loading' && playlists.playlists.length === 0)

watch(
    open,
    (value) => {
        if (value) {
            error.value = null
            busy.value = null
            void playlists.load()
        }
    },
    { immediate: true },
)

async function add(playlist: Playlist): Promise<void> {
    if (!playlist.id || busy.value) {
        return
    }

    busy.value = playlist.id
    error.value = null
    try {
        const changed = await playlists.addTracks(playlist.id, props.trackIds)
        open.value = false
        emit('added', changed ?? playlist)
    } catch (failure) {
        const code = problemCodeOf(failure)
        error.value =
            code === ProblemCodeObject.PlaylistFull
                ? t('playlists.errors.full')
                : code === ProblemCodeObject.PlaylistNot_found
                  ? t('playlists.errors.notFound')
                  : code === ProblemCodeObject.TrackNot_found
                    ? t('playlists.errors.trackNotFound')
                    : t('playlists.errors.addFailed')
        if (code === ProblemCodeObject.PlaylistNot_found) {
            playlists.forget(playlist.id)
        }
    } finally {
        busy.value = null
    }
}

function created(playlist: Playlist): void {
    open.value = false
    emit('added', playlist)
}
</script>

<template>
    <BaseDialog v-model:open="open" :title="t('playlists.addDialog.title')">
        <BaseAlert v-if="error" tone="error" :message="error" />
        <p v-if="loading">{{ t('playlists.addDialog.loading') }}</p>
        <BaseAlert
            v-else-if="playlists.status === 'error'"
            tone="error"
            :message="t('playlists.errors.loadFailed')"
        />
        <p v-else-if="playlists.playlists.length === 0">{{ t('playlists.addDialog.empty') }}</p>
        <ul v-else data-part="playlist-choices" :aria-label="t('playlists.addDialog.listLabel')">
            <li v-for="playlist in playlists.playlists" :key="playlist.id ?? ''">
                <button
                    type="button"
                    data-part="playlist-choice"
                    :data-state="busy === playlist.id ? 'busy' : undefined"
                    :disabled="busy !== null"
                    @click="add(playlist)"
                >
                    <PlaylistCover :id="playlist.id" :cover="playlist.cover" />
                    <span data-part="label">{{ playlist.name }}</span>
                    <span data-part="playlist-choice-count">
                        {{
                            t('library.songCount', {
                                count: format.number(playlist.trackCount ?? 0),
                            })
                        }}
                    </span>
                </button>
            </li>
        </ul>
        <template #footer>
            <BaseButton :label="t('actions.cancel')" @click="open = false" />
            <BaseButton
                variant="primary"
                icon="add"
                :label="t('playlists.addDialog.newPlaylist')"
                :disabled="busy !== null"
                @click="creating = true"
            />
        </template>
    </BaseDialog>
    <PlaylistEditDialog v-model:open="creating" :track-ids="trackIds" @saved="created" />
</template>
