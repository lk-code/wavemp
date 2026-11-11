<script setup lang="ts">
import { computed, useId } from 'vue'

export interface SelectOption {
    readonly value: string
    readonly label: string
    readonly disabled?: boolean
}

/** Native select with label, optional help text and error; v-model is the value of an option. */
const props = withDefaults(
    defineProps<{
        label: string
        options: readonly SelectOption[]
        disabled?: boolean
        hint?: string
        error?: string
    }>(),
    { disabled: false, hint: undefined, error: undefined },
)

const value = defineModel<string>({ default: '' })

const hintId = useId()
const errorId = useId()
const describedBy = computed(
    () =>
        [props.error ? errorId : undefined, props.hint ? hintId : undefined]
            .filter(Boolean)
            .join(' ') || undefined,
)
</script>

<template>
    <label data-part="field" :data-state="error ? 'invalid' : undefined">
        <span data-part="label">{{ label }}</span>
        <select
            v-model="value"
            data-part="select"
            :disabled="disabled"
            :aria-invalid="error ? 'true' : undefined"
            :aria-describedby="describedBy"
        >
            <option
                v-for="option in options"
                :key="option.value"
                :value="option.value"
                :disabled="option.disabled"
            >
                {{ option.label }}
            </option>
        </select>
        <span v-if="hint" :id="hintId" data-part="field-hint">{{ hint }}</span>
        <span v-if="error" :id="errorId" data-part="field-error">{{ error }}</span>
    </label>
</template>
