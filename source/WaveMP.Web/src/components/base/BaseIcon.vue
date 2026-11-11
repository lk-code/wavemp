<script setup lang="ts">
import type { IconSlotName } from '@/themes/iconSlots'

/** Icon of the theme API: the slot decides which graphic the theme shows (see THEMING.md). */
defineProps<{
    name: IconSlotName
}>()
</script>

<template>
    <span data-part="icon" :data-icon="name" aria-hidden="true">
        <span data-part="icon-glyph"></span>
    </span>
</template>
