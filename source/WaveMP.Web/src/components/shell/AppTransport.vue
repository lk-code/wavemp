<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseSlider from '@/components/base/BaseSlider.vue'
import { useFormat } from '@/composables/useFormat'
import { useMediaQuery } from '@/composables/useMediaQuery'
import { desktopMediaQuery } from '@/themes/breakpoints'
import { transportIcons, transportStateTokens, type TransportState } from './transport'

/**
 * Transport bar: seek bar, mini player and playback controls. It is controlled through props and
 * events only; on mobile, the same DOM becomes the mini player and the full-screen player.
 */
const props = withDefaults(
    defineProps<{
        state: TransportState
        /** Full-screen player on mobile. */
        expanded?: boolean
    }>(),
    { expanded: false },
)

const emit = defineEmits<{
    play: []
    pause: []
    stop: []
    previous: []
    next: []
    seek: [position: number]
    toggleShuffle: []
    cycleRepeat: []
    toggleMute: []
    changeVolume: [volume: number]
    compact: []
    showNowPlaying: []
    expand: []
    collapse: []
}>()

const { t } = useI18n()
const format = useFormat()
const isDesktop = useMediaQuery(desktopMediaQuery)

const idle = computed(() => props.state.playback === 'idle')
const playing = computed(() => props.state.playback === 'playing')
const icons = computed(() => transportIcons(props.state))
const tokens = computed(() => transportStateTokens(props.state.playback, props.expanded))

const repeatLabel = computed(() => {
    switch (props.state.repeat) {
        case 'all':
            return t('player.repeatAll')
        case 'one':
            return t('player.repeatOne')
        default:
            return t('player.repeatOff')
    }
})

const positionText = computed(() =>
    t('player.position', {
        position: format.duration(props.state.position),
        duration: format.duration(props.state.duration),
    }),
)

function onMiniPlayer(): void {
    if (isDesktop.value) {
        emit('showNowPlaying')
    } else if (!props.expanded) {
        emit('expand')
    }
}
</script>

<template>
    <section data-part="transport" :aria-label="t('player.label')" :data-state="tokens">
        <div data-part="seek" :data-state="idle ? 'disabled' : undefined">
            <BaseSlider
                :model-value="state.position"
                :min="0"
                :max="Math.max(state.duration, 1)"
                :step="1"
                :large-step="10"
                :label="t('player.seek')"
                :value-text="positionText"
                :disabled="idle"
                @update:model-value="emit('seek', $event)"
            />
        </div>
        <button
            type="button"
            data-part="now-playing-mini"
            :data-state="state.title ? undefined : 'empty'"
            :aria-expanded="isDesktop ? undefined : expanded ? 'true' : 'false'"
            @click="onMiniPlayer"
        >
            <BaseCover />
            <span data-part="now-playing-meta">
                <span data-part="now-playing-title">{{
                    state.title ?? t('player.nothingPlaying')
                }}</span>
                <span data-part="now-playing-artist">{{ state.artist }}</span>
            </span>
            <span data-part="label">{{
                isDesktop ? t('player.showNowPlaying') : t('player.open')
            }}</span>
        </button>
        <div data-part="transport-controls" role="group" :aria-label="t('player.controls')">
            <BaseButton
                part="btn-shuffle"
                variant="icon"
                icon="shuffle"
                :label="t('player.shuffle')"
                :pressed="state.shuffle"
                :state="state.shuffle ? 'on' : 'off'"
                @click="emit('toggleShuffle')"
            />
            <BaseButton
                part="btn-repeat"
                variant="icon"
                :icon="icons.repeat"
                :label="repeatLabel"
                :pressed="state.repeat !== 'off'"
                :state="state.repeat"
                @click="emit('cycleRepeat')"
            />
            <BaseButton
                part="btn-stop"
                variant="icon"
                icon="stop"
                :label="t('player.stop')"
                :disabled="idle"
                @click="emit('stop')"
            />
            <BaseButton
                part="btn-prev"
                variant="icon"
                icon="previous"
                :label="t('player.previous')"
                :disabled="idle"
                @click="emit('previous')"
            />
            <BaseButton
                part="btn-play"
                variant="round"
                :icon="icons.play"
                :label="playing ? t('player.pause') : t('player.play')"
                :state="state.playback"
                :disabled="idle"
                @click="playing ? emit('pause') : emit('play')"
            />
            <BaseButton
                part="btn-next"
                variant="icon"
                icon="next"
                :label="t('player.next')"
                :disabled="idle"
                @click="emit('next')"
            />
            <BaseButton
                part="btn-mute"
                variant="icon"
                :icon="icons.volume"
                :label="t('player.mute')"
                :pressed="state.muted"
                :state="state.muted ? 'muted' : 'unmuted'"
                @click="emit('toggleMute')"
            />
            <div data-part="volume" :data-state="state.muted ? 'muted' : undefined">
                <BaseSlider
                    :model-value="state.volume"
                    :step="0.05"
                    :large-step="0.1"
                    :label="t('player.volume')"
                    :value-text="format.percent(state.volume)"
                    @update:model-value="emit('changeVolume', $event)"
                />
            </div>
        </div>
        <div data-part="mode-switch" role="group" :aria-label="t('player.modes')">
            <!-- The compact mode follows in a later step. -->
            <BaseButton
                part="btn-compact"
                variant="icon"
                icon="compact"
                :label="t('player.compactMode')"
                disabled
                @click="emit('compact')"
            />
        </div>
        <BaseButton
            part="btn-collapse"
            variant="icon"
            icon="collapse-player"
            :label="t('player.close')"
            @click="emit('collapse')"
        />
    </section>
</template>
