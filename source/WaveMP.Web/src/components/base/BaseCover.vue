<script setup lang="ts">
import BaseIcon from './BaseIcon.vue'

/** Cover of a track or album. Without an image it shows the placeholder of the theme. */
withDefaults(
    defineProps<{
        placeholder?: boolean
    }>(),
    { placeholder: true },
)
</script>

<template>
    <span
        data-part="cover"
        :data-state="placeholder ? 'placeholder' : undefined"
        aria-hidden="true"
    >
        <slot>
            <BaseIcon name="cover-placeholder" />
        </slot>
    </span>
</template>
