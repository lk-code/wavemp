<script setup lang="ts">
import { computed, ref } from 'vue'
import {
    ratio,
    valueForKey,
    valueFromPointer,
    valueFromPointerVertical,
    type SliderOptions,
} from './slider'

const props = withDefaults(
    defineProps<{
        /** Accessible name of the slider. */
        label: string
        min?: number
        max?: number
        step?: number
        largeStep?: number
        /** Readable value for assistive technology, e.g. "1:05 of 3:20" or "80 %". */
        valueText?: string
        disabled?: boolean
        /** Vertical sliders grow from the bottom (min) to the top (max), e.g. in the equalizer. */
        orientation?: 'horizontal' | 'vertical'
    }>(),
    {
        min: 0,
        max: 1,
        step: 0.01,
        largeStep: 0.1,
        valueText: undefined,
        disabled: false,
        orientation: 'horizontal',
    },
)

const value = defineModel<number>({ required: true })

const thumb = ref<HTMLElement | null>(null)
const dragging = ref(false)

const options = computed<SliderOptions>(() => ({
    min: props.min,
    max: props.max,
    step: props.step,
    largeStep: props.largeStep,
}))
const position = computed(() => ratio(value.value, options.value))

function update(next: number): void {
    if (next !== value.value) {
        value.value = next
    }
}

function fromPointer(event: PointerEvent): void {
    const element = event.currentTarget as HTMLElement
    const rect = element.getBoundingClientRect()
    update(
        props.orientation === 'vertical'
            ? valueFromPointerVertical(event.clientY, rect, options.value)
            : valueFromPointer(event.clientX, rect, options.value),
    )
}

function onPointerDown(event: PointerEvent): void {
    if (props.disabled || event.button !== 0) {
        return
    }

    event.preventDefault()
    const element = event.currentTarget as HTMLElement
    element.setPointerCapture?.(event.pointerId)
    dragging.value = true
    thumb.value?.focus()
    fromPointer(event)
}

function onPointerMove(event: PointerEvent): void {
    if (dragging.value) {
        fromPointer(event)
    }
}

function onPointerEnd(): void {
    dragging.value = false
}

function onKeydown(event: KeyboardEvent): void {
    if (props.disabled) {
        return
    }

    const next = valueForKey(event.key, value.value, options.value)
    if (next !== null) {
        event.preventDefault()
        update(next)
    }
}
</script>

<template>
    <div
        data-part="slider"
        :data-orientation="orientation"
        :data-state="
            [disabled ? 'disabled' : undefined, dragging ? 'dragging' : undefined]
                .filter(Boolean)
                .join(' ') || undefined
        "
        :style="{ '--wmp-slider-value': position }"
        @pointerdown="onPointerDown"
        @pointermove="onPointerMove"
        @pointerup="onPointerEnd"
        @pointercancel="onPointerEnd"
        @lostpointercapture="onPointerEnd"
    >
        <div data-part="slider-track">
            <div data-part="slider-fill"></div>
        </div>
        <div
            ref="thumb"
            data-part="slider-thumb"
            role="slider"
            :tabindex="disabled ? -1 : 0"
            :aria-label="label"
            :aria-valuemin="min"
            :aria-valuemax="max"
            :aria-valuenow="value"
            :aria-valuetext="valueText"
            :aria-disabled="disabled ? 'true' : undefined"
            :aria-orientation="orientation"
            @keydown="onKeydown"
        ></div>
    </div>
</template>
