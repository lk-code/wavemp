<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { PlaylistCover } from '@/api/generated/models'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import LibraryCover from '@/components/library/LibraryCover.vue'
import { useInView } from '@/composables/useInView'
import { useMediaQuery } from '@/composables/useMediaQuery'
import { coverSlideMilliseconds, coverViewOf, nextSlide } from '@/playlists/presentation'

/**
 * Cover of a playlist: its own picture, otherwise the covers of up to four of its tracks, one after
 * the other (the active slide has data-state="active"; the theme fades them), otherwise the
 * placeholder. The slides only change while the cover is in view and nobody asked for reduced
 * motion.
 */
const props = defineProps<{
    id: string | null | undefined
    cover: PlaylistCover | null | undefined
}>()

const view = computed(() => coverViewOf(props.cover))
const trackIds = computed(() => (view.value.kind === 'tracks' ? view.value.trackIds : []))
const slides = ref<HTMLElement | null>(null)
const visible = useInView(slides)
const reducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)')
const active = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

function stop(): void {
    if (timer !== null) {
        clearInterval(timer)
        timer = null
    }
}

watch(
    () => [trackIds.value.length, visible.value, reducedMotion.value] as const,
    ([count, inView, reduced]) => {
        stop()
        if (active.value >= count) {
            active.value = 0
        }

        if (count > 1 && inView && !reduced) {
            timer = setInterval(() => {
                active.value = nextSlide(active.value, trackIds.value.length)
            }, coverSlideMilliseconds)
        }
    },
    { immediate: true },
)

onBeforeUnmount(stop)
</script>

<template>
    <LibraryCover
        v-if="view.kind === 'custom'"
        kind="playlist"
        :id="id"
        :available="true"
        :version="view.version"
    />
    <span
        v-else-if="view.kind === 'tracks'"
        ref="slides"
        data-part="playlist-cover"
        :data-count="trackIds.length"
        aria-hidden="true"
    >
        <span
            v-for="(trackId, index) in trackIds"
            :key="trackId"
            data-part="playlist-cover-slide"
            :data-state="index === active ? 'active' : undefined"
        >
            <LibraryCover kind="track" :id="trackId" :available="true" />
        </span>
    </span>
    <BaseCover v-else :placeholder="false">
        <BaseIcon name="playlist" />
    </BaseCover>
</template>
