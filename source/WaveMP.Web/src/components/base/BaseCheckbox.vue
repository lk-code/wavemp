<script setup lang="ts">
import { computed, useId } from 'vue'

/** Checkbox with label, optional help text and error; v-model is the checked state. */
const props = withDefaults(
    defineProps<{
        label: string
        disabled?: boolean
        hint?: string
        error?: string
    }>(),
    { disabled: false, hint: undefined, error: undefined },
)

const checked = defineModel<boolean>({ default: false })

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
    <label data-part="field" data-variant="checkbox" :data-state="error ? 'invalid' : undefined">
        <input
            v-model="checked"
            type="checkbox"
            data-part="checkbox"
            :disabled="disabled"
            :aria-invalid="error ? 'true' : undefined"
            :aria-describedby="describedBy"
        />
        <span data-part="label">{{ label }}</span>
        <span v-if="hint" :id="hintId" data-part="field-hint">{{ hint }}</span>
        <span v-if="error" :id="errorId" data-part="field-error">{{ error }}</span>
    </label>
</template>
