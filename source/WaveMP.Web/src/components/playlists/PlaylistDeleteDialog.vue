<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { Playlist } from '@/api/generated/models'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'

/** Asks before a playlist is deleted; its tracks stay in the library. */
defineProps<{
    playlist: Playlist | null
    busy?: boolean
    error?: string | null
}>()

const emit = defineEmits<{
    confirm: []
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
</script>

<template>
    <BaseDialog v-model:open="open" :title="t('playlists.deleteDialog.title')">
        <BaseAlert v-if="error" tone="error" :message="error" />
        <p>{{ t('playlists.deleteDialog.text', { name: playlist?.name ?? '' }) }}</p>
        <template #footer>
            <BaseButton :label="t('actions.cancel')" @click="open = false" />
            <BaseButton
                variant="primary"
                :label="t('playlists.deleteDialog.confirm')"
                :disabled="busy"
                @click="emit('confirm')"
            />
        </template>
    </BaseDialog>
</template>
