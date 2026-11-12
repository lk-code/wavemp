<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ProblemCodeObject, type Playlist } from '@/api/generated/models'
import { problemCodeOf, statusOf, validationErrorsOf } from '@/api/problems'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import { validationMessagesByField } from '@/i18n/validation'
import { playlistNameMaxLength } from '@/playlists/presentation'
import { usePlaylistsStore } from '@/stores/playlists'

/**
 * Creates a playlist, optionally with its first tracks, or renames one. The server trims the name
 * and checks it again.
 */
const props = withDefaults(
    defineProps<{
        /** The playlist to rename; null creates a new one. */
        playlist?: Playlist | null
        /** Tracks a new playlist starts with. */
        trackIds?: readonly string[]
    }>(),
    { playlist: null, trackIds: () => [] },
)

const emit = defineEmits<{
    saved: [playlist: Playlist]
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
const playlists = usePlaylistsStore()
const formId = useId()

const name = ref('')
const nameError = ref<string | undefined>(undefined)
const error = ref<string | null>(null)
const saving = ref(false)

const editing = computed(() => props.playlist !== null)

watch(
    open,
    (value) => {
        if (value) {
            name.value = props.playlist?.name ?? ''
            nameError.value = undefined
            error.value = null
        }
    },
    { immediate: true },
)

function report(failure: unknown): void {
    const code = problemCodeOf(failure)
    if (code === ProblemCodeObject.PlaylistNot_found) {
        error.value = t('playlists.errors.notFound')
    } else if (code === ProblemCodeObject.TrackNot_found) {
        error.value = t('playlists.errors.trackNotFound')
    } else if (code === ProblemCodeObject.PlaylistFull) {
        error.value = t('playlists.errors.full')
    } else if (statusOf(failure) === 400) {
        const messages = validationMessagesByField(
            validationErrorsOf(failure),
            { name: 'name' } as const,
            (key, parameters) => t(key, parameters),
        )
        nameError.value = messages.name ?? t('playlists.errors.nameInvalid')
    } else {
        error.value = t('playlists.errors.saveFailed')
    }
}

async function save(): Promise<void> {
    if (saving.value) {
        return
    }

    nameError.value = undefined
    error.value = null
    const trimmed = name.value.trim()
    if (trimmed === '') {
        nameError.value = t('playlists.errors.nameRequired')
        return
    }

    saving.value = true
    try {
        const saved = props.playlist?.id
            ? await playlists.rename(props.playlist.id, trimmed)
            : await playlists.create(trimmed, props.trackIds)
        open.value = false
        if (saved) {
            emit('saved', saved)
        }
    } catch (failure) {
        report(failure)
    } finally {
        saving.value = false
    }
}
</script>

<template>
    <BaseDialog
        v-model:open="open"
        :title="editing ? t('playlists.dialog.renameTitle') : t('playlists.dialog.createTitle')"
    >
        <form :id="formId" data-part="form" novalidate @submit.prevent="save">
            <BaseAlert v-if="error" tone="error" :message="error" />
            <BaseTextField
                v-model="name"
                :label="t('playlists.dialog.name')"
                :error="nameError"
                :maxlength="playlistNameMaxLength"
                autocomplete="off"
                required
            />
        </form>
        <template #footer>
            <BaseButton :label="t('actions.cancel')" @click="open = false" />
            <BaseButton
                type="submit"
                variant="primary"
                :form="formId"
                :label="editing ? t('playlists.dialog.rename') : t('playlists.dialog.create')"
                :disabled="saving"
            />
        </template>
    </BaseDialog>
</template>
