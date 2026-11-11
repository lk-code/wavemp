<script setup lang="ts">
import { computed } from 'vue'
import BaseIcon from './BaseIcon.vue'
import type { IconSlotName } from '@/themes/iconSlots'
import type { PartName } from '@/themes/parts'

export type ButtonVariant = 'default' | 'primary' | 'icon' | 'round'

const props = withDefaults(
    defineProps<{
        /** Accessible text; always rendered as data-part="label", themes may hide it visually. */
        label: string
        part?: PartName
        variant?: ButtonVariant
        icon?: IconSlotName
        /** Adds the dropdown icon after the label. */
        dropdown?: boolean
        /** Shows the label as tooltip; default for icon and round buttons. */
        tooltip?: boolean
        /** Toggle state (aria-pressed); undefined for buttons that are no toggles. */
        pressed?: boolean
        /** data-state tokens, e.g. "playing" or "on". */
        state?: string
        disabled?: boolean
    }>(),
    {
        part: 'button',
        variant: 'default',
        icon: undefined,
        dropdown: false,
        tooltip: undefined,
        pressed: undefined,
        state: undefined,
        disabled: false,
    },
)

const title = computed(() => {
    const showTooltip = props.tooltip ?? (props.variant === 'icon' || props.variant === 'round')
    return showTooltip ? props.label : undefined
})
</script>

<template>
    <button
        type="button"
        :data-part="part"
        :data-variant="variant"
        :data-state="state"
        :title="title"
        :aria-pressed="pressed === undefined ? undefined : pressed ? 'true' : 'false'"
        :disabled="disabled"
    >
        <BaseIcon v-if="icon" :name="icon" />
        <span data-part="label">{{ label }}</span>
        <BaseIcon v-if="dropdown" name="dropdown" />
    </button>
</template>
