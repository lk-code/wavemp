<script setup lang="ts">
/** One detail of an album group or a detail page: a label and its value (default slot). */
defineProps<{
    /** data-field of the detail, e.g. "artist" or "year". */
    field: string
    label: string
}>()
</script>

<template>
    <span data-part="meta-item" :data-field="field">
        <span data-part="label">{{ label }}</span>
        <span data-part="meta-value"><slot /></span>
    </span>
</template>
