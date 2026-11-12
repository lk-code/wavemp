<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCover from '@/components/base/BaseCover.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import VideoStage from './VideoStage.vue'
import { artistLabelKey } from '@/library/presentation'
import type { Track } from '@/api/generated/models'

/**
 * The stage of Now Playing: the cover, or the video of tracks with one, with title, artist and
 * album of the current track, or a hint while nothing plays. The video button switches between
 * video and cover; the visualization follows in a later step.
 */
const props = withDefaults(
    defineProps<{
        track?: Track
        /** Whether tracks with a video show it (video mode of the player). */
        videoMode?: boolean
    }>(),
    { track: undefined, videoMode: false },
)

const emit = defineEmits<{
    toggleVideo: []
    /** The video element of the stage is shown; the player plays videos there. */
    attachVideo: [element: HTMLVideoElement]
    /** The video element closes. */
    detachVideo: [element: HTMLVideoElement]
}>()

const { t } = useI18n()

const hasVideo = computed(() => props.track?.hasVideo === true)
const showsVideo = computed(() => hasVideo.value && props.videoMode)

const subtitle = computed(() => {
    const key = artistLabelKey(props.track?.artist)
    const artist = key ? t(key) : (props.track?.artist?.name ?? '')
    return [artist, props.track?.album?.title ?? ''].filter((part) => part !== '').join(' – ')
})
</script>

<template>
    <div data-part="stage" :data-state="showsVideo ? 'video' : 'cover'">
        <VideoStage
            v-if="showsVideo"
            @attach="emit('attachVideo', $event)"
            @detach="emit('detachVideo', $event)"
        />
        <LibraryCover
            v-else-if="track?.id"
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
        <div
            v-if="hasVideo"
            data-part="stage-actions"
            role="group"
            :aria-label="t('nowPlaying.stageActions')"
        >
            <BaseButton
                part="btn-video"
                icon="view-video"
                :label="t('nowPlaying.showVideo')"
                :pressed="showsVideo"
                @click="emit('toggleVideo')"
            />
        </div>
    </div>
</template>
