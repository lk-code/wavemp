<script setup lang="ts">
withDefaults(
    defineProps<{
        label: string
        placeholder?: string
        type?: 'text' | 'email' | 'url' | 'number' | 'password'
        disabled?: boolean
    }>(),
    { placeholder: undefined, type: 'text', disabled: false },
)

const value = defineModel<string>({ default: '' })
</script>

<template>
    <label data-part="field">
        <span data-part="label">{{ label }}</span>
        <input
            v-model="value"
            data-part="input"
            :type="type"
            :placeholder="placeholder"
            :disabled="disabled"
        />
    </label>
</template>
