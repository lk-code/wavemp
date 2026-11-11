<script setup lang="ts">
import { computed, useId } from 'vue'

const props = withDefaults(
    defineProps<{
        label: string
        placeholder?: string
        type?: 'text' | 'email' | 'url' | 'number' | 'password'
        disabled?: boolean
        required?: boolean
        maxlength?: number
        autocomplete?: string
        /** Help text below the input. */
        hint?: string
        /** Error message; marks the field as invalid. */
        error?: string
    }>(),
    {
        placeholder: undefined,
        type: 'text',
        disabled: false,
        required: false,
        maxlength: undefined,
        autocomplete: undefined,
        hint: undefined,
        error: undefined,
    },
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
        <input
            v-model="value"
            data-part="input"
            :type="type"
            :placeholder="placeholder"
            :disabled="disabled"
            :required="required"
            :maxlength="maxlength"
            :autocomplete="autocomplete"
            :aria-invalid="error ? 'true' : undefined"
            :aria-describedby="describedBy"
        />
        <span v-if="hint" :id="hintId" data-part="field-hint">{{ hint }}</span>
        <span v-if="error" :id="errorId" data-part="field-error">{{ error }}</span>
    </label>
</template>
