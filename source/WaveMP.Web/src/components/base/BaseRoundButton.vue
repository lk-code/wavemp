<script setup lang="ts">
import BaseButton from './BaseButton.vue'
import type { IconSlotName } from '@/themes/iconSlots'
import type { PartName } from '@/themes/parts'

/** Round glass button (variant "round"); the label is the tooltip and stays accessible. */
withDefaults(
    defineProps<{
        label: string
        icon: IconSlotName
        part?: PartName
        pressed?: boolean
        state?: string
        disabled?: boolean
    }>(),
    {
        part: 'button',
        pressed: undefined,
        state: undefined,
        disabled: false,
    },
)
</script>

<template>
    <BaseButton
        variant="round"
        :part="part"
        :icon="icon"
        :label="label"
        :pressed="pressed"
        :state="state"
        :disabled="disabled"
    />
</template>
