<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import { canFullscreen, enterFullscreen, exitFullscreen, isFullscreen } from '@/player/fullscreen'

/**
 * The video of the current track on the Now Playing stage, muted: the player plays the audio and
 * the video follows it. The element is handed to the player when it is shown (attach) and taken
 * back before it closes (detach); the song simply goes on. While the video view is active, a double
 * click or the button shows the stage in full screen.
 */
const props = withDefaults(
    defineProps<{
        /** Whether the stage shows the video view. */
        active?: boolean
    }>(),
    { active: true },
)

const emit = defineEmits<{
    attach: [element: HTMLVideoElement]
    detach: [element: HTMLVideoElement]
}>()

const { t } = useI18n()

const container = ref<HTMLElement | null>(null)
const video = ref<HTMLVideoElement | null>(null)
const fullscreen = ref(false)
const fullscreenAvailable = ref(false)

function update(): void {
    if (container.value && video.value) {
        fullscreen.value = isFullscreen(container.value, video.value)
    }
}

async function toggleFullscreen(): Promise<void> {
    if (!container.value || !video.value || !fullscreenAvailable.value) {
        return
    }

    // A hidden video never enters full screen; one in full screen may always leave it.
    if (!props.active && !isFullscreen(container.value, video.value)) {
        return
    }

    if (isFullscreen(container.value, video.value)) {
        await exitFullscreen(container.value, video.value)
    } else {
        await enterFullscreen(container.value, video.value)
    }

    update()
}

onMounted(() => {
    const element = video.value!
    fullscreenAvailable.value = canFullscreen(container.value!, element)
    document.addEventListener('fullscreenchange', update)
    element.addEventListener('webkitbeginfullscreen', update)
    element.addEventListener('webkitendfullscreen', update)
    emit('attach', element)
})

onBeforeUnmount(() => {
    const element = video.value!
    document.removeEventListener('fullscreenchange', update)
    element.removeEventListener('webkitbeginfullscreen', update)
    element.removeEventListener('webkitendfullscreen', update)
    if (fullscreen.value) {
        void exitFullscreen(container.value!, element)
    }

    emit('detach', element)
})
</script>

<template>
    <div
        ref="container"
        data-part="video-stage"
        :data-state="fullscreen ? 'fullscreen' : undefined"
        @dblclick="toggleFullscreen"
    >
        <video
            ref="video"
            data-part="video"
            muted
            playsinline
            disablepictureinpicture
            :aria-label="t('nowPlaying.video')"
        ></video>
        <BaseButton
            part="btn-fullscreen"
            variant="icon"
            :icon="fullscreen ? 'exit-fullscreen' : 'fullscreen'"
            :label="fullscreen ? t('nowPlaying.exitFullscreen') : t('nowPlaying.fullscreen')"
            :disabled="!fullscreenAvailable || (!active && !fullscreen)"
            @click.stop="toggleFullscreen"
            @dblclick.stop
        />
    </div>
</template>
