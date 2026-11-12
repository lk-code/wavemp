<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCover from '@/components/base/BaseCover.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import StageVisualization from './StageVisualization.vue'
import VideoStage from './VideoStage.vue'
import { artistLabelKey } from '@/library/presentation'
import { PlayerViewModeObject, type PlayerViewMode, type Track } from '@/api/generated/models'
import type { StageView, VideoState } from '@/player/videoSync'

/**
 * The stage of Now Playing: the cover, the video or the visualization of the current track, with
 * title, artist and album, or a hint while nothing plays. The three views lie on top of each other
 * and fade into each other; the buttons choose the view, the music goes on. The video view stays in
 * the page as long as the track has a video, so switching back to it is quick.
 */
const props = withDefaults(
    defineProps<{
        track?: Track
        /** The view mode chosen by the profile. */
        mode?: PlayerViewMode
        /** The view the stage shows. */
        view?: StageView
        /** State of the video of the track. */
        videoState?: VideoState
    }>(),
    {
        track: undefined,
        mode: PlayerViewModeObject.Video,
        view: 'cover',
        videoState: 'idle',
    },
)

const emit = defineEmits<{
    selectMode: [mode: PlayerViewMode]
    /** The video element of the stage is shown; the player shows videos there. */
    attachVideo: [element: HTMLVideoElement]
    /** The video element closes. */
    detachVideo: [element: HTMLVideoElement]
}>()

const { t } = useI18n()

const hasVideo = computed(() => props.track?.hasVideo === true)

/** data-state of the video view: shown, still loading or positioning, or failed. */
const videoViewState = computed(() => {
    if (props.view === 'video') {
        return 'active'
    }

    return props.videoState === 'loading' || props.videoState === 'failed'
        ? props.videoState
        : undefined
})

const videoFailed = computed(
    () =>
        hasVideo.value &&
        props.mode === PlayerViewModeObject.Video &&
        props.videoState === 'failed',
)

/**
 * The pressed button: the chosen mode, but the cover for a track without a video or with a failed
 * one. While the video loads, its button is pressed and the cover stays until the video is ready.
 */
const selected = computed<PlayerViewMode>(() =>
    props.mode === PlayerViewModeObject.Video && (!hasVideo.value || videoFailed.value)
        ? PlayerViewModeObject.Cover
        : props.mode,
)

const subtitle = computed(() => {
    const key = artistLabelKey(props.track?.artist)
    const artist = key ? t(key) : (props.track?.artist?.name ?? '')
    return [artist, props.track?.album?.title ?? ''].filter((part) => part !== '').join(' – ')
})
</script>

<template>
    <div data-part="stage" :data-state="view">
        <div data-part="stage-views">
            <div
                data-part="stage-view"
                data-kind="cover"
                :data-state="view === 'cover' ? 'active' : undefined"
                :aria-hidden="view === 'cover' ? undefined : 'true'"
            >
                <LibraryCover
                    v-if="track?.id"
                    kind="track"
                    :id="track.id"
                    :available="track.hasCover ?? false"
                />
                <BaseCover v-else />
            </div>
            <div
                v-if="hasVideo"
                data-part="stage-view"
                data-kind="video"
                :data-state="videoViewState"
                :aria-hidden="view === 'video' ? undefined : 'true'"
            >
                <VideoStage
                    :active="view === 'video'"
                    @attach="emit('attachVideo', $event)"
                    @detach="emit('detachVideo', $event)"
                />
            </div>
            <div
                data-part="stage-view"
                data-kind="visualization"
                :data-state="view === 'visualization' ? 'active' : undefined"
                :aria-hidden="view === 'visualization' ? undefined : 'true'"
            >
                <StageVisualization />
            </div>
        </div>
        <p v-if="videoFailed" data-part="stage-notice" role="status">
            {{ t('nowPlaying.videoFailed') }}
        </p>
        <div v-if="track" data-part="stage-info">
            <p data-part="stage-title">{{ track.title }}</p>
            <p data-part="stage-subtitle">{{ subtitle }}</p>
        </div>
        <div v-else data-part="empty-state">
            <p data-part="empty-state-title">{{ t('nowPlaying.emptyTitle') }}</p>
            <p data-part="empty-state-text">{{ t('nowPlaying.emptyText') }}</p>
        </div>
        <div
            v-if="track"
            data-part="stage-actions"
            role="group"
            :aria-label="t('nowPlaying.stageActions')"
        >
            <BaseButton
                part="btn-view-cover"
                icon="view-cover"
                :label="t('nowPlaying.viewCover')"
                :pressed="selected === PlayerViewModeObject.Cover"
                @click="emit('selectMode', PlayerViewModeObject.Cover)"
            />
            <BaseButton
                part="btn-view-video"
                icon="view-video"
                :label="t('nowPlaying.viewVideo')"
                :pressed="selected === PlayerViewModeObject.Video"
                :disabled="!hasVideo"
                @click="emit('selectMode', PlayerViewModeObject.Video)"
            />
            <BaseButton
                part="btn-view-visualization"
                icon="view-visualization"
                :label="t('nowPlaying.viewVisualization')"
                :pressed="selected === PlayerViewModeObject.Visualization"
                @click="emit('selectMode', PlayerViewModeObject.Visualization)"
            />
        </div>
    </div>
</template>
