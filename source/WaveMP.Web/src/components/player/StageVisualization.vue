<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseIcon from '@/components/base/BaseIcon.vue'

/**
 * The visualization of the Now Playing stage. For now a placeholder; the visualizations that draw
 * the music follow in a later step and take over this container.
 */
const { t } = useI18n()
</script>

<template>
    <div data-part="visualization" data-state="placeholder">
        <BaseIcon name="view-visualization" />
        <span data-part="label">{{ t('nowPlaying.visualizationPlaceholder') }}</span>
    </div>
</template>
