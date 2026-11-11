<script setup lang="ts">
import { computed, nextTick, ref, useId } from 'vue'
import { colorTabStop, nextColorIndex } from './colorPicker'

export interface ColorOption {
    /** Color as #rrggbb. */
    readonly value: string
    /** Name of the color, e.g. "Blue"; shown as tooltip and read by assistive technology. */
    readonly label: string
}

/** Selection of a color from swatches (radio group); v-model is the value, null for none. */
const props = defineProps<{
    label: string
    options: readonly ColorOption[]
}>()

const value = defineModel<string | null>({ default: null })

const labelId = useId()
const picker = ref<HTMLElement | null>(null)

const selected = computed(() =>
    props.options.findIndex((option) => option.value.toLowerCase() === value.value?.toLowerCase()),
)
const tabStop = computed(() => colorTabStop(selected.value))

async function onKeydown(event: KeyboardEvent): Promise<void> {
    const next = nextColorIndex(event.key, selected.value, props.options.length)
    const option = next === null ? undefined : props.options[next]
    if (!option) {
        return
    }

    event.preventDefault()
    value.value = option.value
    await nextTick()
    picker.value?.querySelectorAll<HTMLElement>('[data-part="color-swatch"]')[next ?? 0]?.focus()
}
</script>

<template>
    <div data-part="field">
        <span :id="labelId" data-part="label">{{ label }}</span>
        <div
            ref="picker"
            data-part="color-picker"
            role="radiogroup"
            :aria-labelledby="labelId"
            @keydown="onKeydown"
        >
            <button
                v-for="(option, index) in options"
                :key="option.value"
                type="button"
                data-part="color-swatch"
                role="radio"
                :aria-checked="index === selected ? 'true' : 'false'"
                :tabindex="index === tabStop ? 0 : -1"
                :title="option.label"
                :style="{ '--wmp-swatch-color': option.value }"
                @click="value = option.value"
            >
                <span data-part="label">{{ option.label }}</span>
            </button>
        </div>
    </div>
</template>
