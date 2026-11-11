<script setup lang="ts">
import BaseIcon from './BaseIcon.vue'

export type AlertTone = 'error' | 'success' | 'info'

/** Message with an icon; errors are announced at once (role alert), others politely (status). */
withDefaults(
    defineProps<{
        message: string
        tone?: AlertTone
    }>(),
    { tone: 'info' },
)
</script>

<template>
    <div data-part="alert" :data-state="tone" :role="tone === 'error' ? 'alert' : 'status'">
        <BaseIcon name="info" />
        <span data-part="label">{{ message }}</span>
    </div>
</template>
