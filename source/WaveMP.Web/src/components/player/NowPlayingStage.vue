<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseCover from '@/components/base/BaseCover.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import { artistLabelKey } from '@/library/presentation'
import type { Track } from '@/api/generated/models'

/**
 * The stage of Now Playing: cover, title, artist and album of the current track, or a hint while
 * nothing plays. Video and visualization follow in later steps.
 */
const props = defineProps<{
    track?: Track
}>()

const { t } = useI18n()

const subtitle = computed(() => {
    const key = artistLabelKey(props.track?.artist)
    const artist = key ? t(key) : (props.track?.artist?.name ?? '')
    return [artist, props.track?.album?.title ?? ''].filter((part) => part !== '').join(' – ')
})
</script>

<template>
    <div data-part="stage">
        <LibraryCover
            v-if="track?.id"
            kind="track"
            :id="track.id"
            :available="track.hasCover ?? false"
        />
        <BaseCover v-else />
        <div v-if="track" data-part="stage-info">
            <p data-part="stage-title">{{ track.title }}</p>
            <p data-part="stage-subtitle">{{ subtitle }}</p>
        </div>
        <div v-else data-part="empty-state">
            <p data-part="empty-state-title">{{ t('nowPlaying.emptyTitle') }}</p>
            <p data-part="empty-state-text">{{ t('nowPlaying.emptyText') }}</p>
        </div>
    </div>
</template>
