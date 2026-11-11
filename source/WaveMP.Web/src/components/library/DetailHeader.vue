<script setup lang="ts">
/**
 * Head of a detail page (album, artist, genre, year, track): a picture (slot media), the name and
 * the details (default slot, meta-items).
 */
defineProps<{
    title: string
}>()
</script>

<template>
    <div data-part="detail-header">
        <slot name="media" />
        <div data-part="detail-info">
            <h2 data-part="detail-title">{{ title }}</h2>
            <slot />
        </div>
    </div>
</template>
