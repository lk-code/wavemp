<script setup lang="ts">
import { ref } from 'vue'
import BaseButton from './BaseButton.vue'
import BaseIcon from './BaseIcon.vue'

defineProps<{
    /** Accessible name of the search field. */
    label: string
    placeholder: string
    /** Label of the button that clears the search. */
    clearLabel: string
}>()

const value = defineModel<string>({ default: '' })
const input = ref<HTMLInputElement | null>(null)

function clear(): void {
    value.value = ''
    input.value?.focus()
}
</script>

<template>
    <div data-part="search" role="search" :data-state="value ? 'filled' : 'empty'">
        <BaseIcon name="search" />
        <input
            ref="input"
            v-model="value"
            data-part="input"
            type="search"
            enterkeyhint="search"
            :aria-label="label"
            :placeholder="placeholder"
            @keydown.esc="clear"
        />
        <BaseButton
            part="btn-clear"
            variant="icon"
            icon="clear"
            :label="clearLabel"
            :hidden="!value"
            @click="clear"
        />
    </div>
</template>
