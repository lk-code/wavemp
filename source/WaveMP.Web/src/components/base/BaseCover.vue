<script setup lang="ts">
import { computed } from 'vue'
import BaseIcon from './BaseIcon.vue'

/** Cover of a track or album. Without an image it shows the placeholder of the theme. */
const props = withDefaults(
    defineProps<{
        placeholder?: boolean
        /** URL of the picture; decorative, the name of the album or track is shown next to it. */
        src?: string | null
    }>(),
    { placeholder: true, src: null },
)

const isPlaceholder = computed(() => !props.src && props.placeholder)
</script>

<template>
    <span
        data-part="cover"
        :data-state="isPlaceholder ? 'placeholder' : undefined"
        aria-hidden="true"
    >
        <img v-if="src" data-part="cover-image" :src="src" alt="" draggable="false" />
        <slot v-else>
            <BaseIcon name="cover-placeholder" />
        </slot>
    </span>
</template>
