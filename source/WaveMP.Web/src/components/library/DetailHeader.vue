<script setup lang="ts">
/**
 * Head of a detail page (album, artist, genre, year, track): a picture (slot media), the name,
 * the details (default slot, meta-items) and actions such as buttons for admins (slot actions).
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
            <div v-if="$slots.actions" data-part="detail-actions">
                <slot name="actions" />
            </div>
        </div>
    </div>
</template>
