<script setup lang="ts">
import { computed } from 'vue'

/**
 * Gauge of a level within a known range, e.g. the used space of the cache. The share (0 to 1) is
 * the runtime variable --wmp-meter-value. Unlike a progress bar it shows no work in progress.
 */
const props = withDefaults(
    defineProps<{
        /** Accessible name of the meter. */
        label: string
        /** Share between 0 and 1. */
        value: number
        /** Readable value for assistive technology, e.g. "1.2 GB of 2 GB used". */
        valueText?: string
    }>(),
    { valueText: undefined },
)

const ratio = computed(() =>
    Number.isFinite(props.value) ? Math.min(1, Math.max(0, props.value)) : 0,
)
</script>

<template>
    <div
        data-part="meter"
        role="meter"
        :aria-label="label"
        aria-valuemin="0"
        aria-valuemax="100"
        :aria-valuenow="Math.round(ratio * 100)"
        :aria-valuetext="valueText"
        :style="{ '--wmp-meter-value': ratio }"
    >
        <div data-part="meter-track">
            <div data-part="meter-fill"></div>
        </div>
    </div>
</template>
