<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'

/** Asks before the cache is cleared. */
defineProps<{
    busy?: boolean
}>()

const emit = defineEmits<{
    confirm: []
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
</script>

<template>
    <BaseDialog v-model:open="open" :title="t('adminCache.clearDialog.title')">
        <p>{{ t('adminCache.clearDialog.text') }}</p>
        <template #footer>
            <BaseButton :label="t('actions.cancel')" @click="open = false" />
            <BaseButton
                variant="primary"
                :label="t('adminCache.clearDialog.confirm')"
                :disabled="busy"
                @click="emit('confirm')"
            />
        </template>
    </BaseDialog>
</template>
