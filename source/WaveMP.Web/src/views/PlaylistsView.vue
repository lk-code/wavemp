<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'

const { t } = useI18n()
</script>

<template>
    <div data-part="view" data-view="playlists">
        <div data-part="empty-state">
            <p data-part="empty-state-title">{{ t('playlists.emptyTitle') }}</p>
            <p data-part="empty-state-text">{{ t('playlists.emptyText') }}</p>
            <!-- Creating playlists follows in step 10. -->
            <BaseButton variant="primary" icon="add" :label="t('playlists.create')" disabled />
        </div>
    </div>
</template>
