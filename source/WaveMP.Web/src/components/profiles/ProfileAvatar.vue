<script setup lang="ts">
/**
 * Initials of a profile on its color. The only place that renders a profile picture, so avatar
 * images can be added here later (roadmap step 21).
 */
defineProps<{
    initials: string
    /** Color of the profile as #rrggbb (runtime variable --wmp-profile-color). */
    color: string
}>()
</script>

<template>
    <span data-part="avatar" aria-hidden="true" :style="{ '--wmp-profile-color': color }">
        <span data-part="avatar-initials">{{ initials }}</span>
    </span>
</template>
