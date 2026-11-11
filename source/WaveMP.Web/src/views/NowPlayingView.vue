<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseCover from '@/components/base/BaseCover.vue'

const { t } = useI18n()
</script>

<template>
    <div data-part="view" data-view="now-playing">
        <!-- The stage shows the cover; video and visualization follow in later steps. -->
        <div data-part="stage">
            <BaseCover />
            <div data-part="empty-state">
                <p data-part="empty-state-title">{{ t('nowPlaying.emptyTitle') }}</p>
                <p data-part="empty-state-text">{{ t('nowPlaying.emptyText') }}</p>
            </div>
        </div>
    </div>
</template>
