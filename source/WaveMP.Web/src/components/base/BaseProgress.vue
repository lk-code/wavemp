<script setup lang="ts">
import { computed } from 'vue'

/**
 * Progress bar. The share of the work done (0 to 1) is the runtime variable --wmp-progress-value;
 * without a value the progress is indeterminate (data-state="indeterminate").
 */
const props = withDefaults(
    defineProps<{
        /** Accessible name of the progress bar. */
        label: string
        /** Share of the work done between 0 and 1; null while it cannot be known. */
        value?: number | null
        /** Readable value for assistive technology, e.g. "12 of 40 files". */
        valueText?: string
    }>(),
    { value: null, valueText: undefined },
)

const ratio = computed(() =>
    props.value === null || !Number.isFinite(props.value)
        ? null
        : Math.min(1, Math.max(0, props.value)),
)
</script>

<template>
    <div
        data-part="progress"
        role="progressbar"
        :aria-label="label"
        aria-valuemin="0"
        aria-valuemax="100"
        :aria-valuenow="ratio === null ? undefined : Math.round(ratio * 100)"
        :aria-valuetext="valueText"
        :data-state="ratio === null ? 'indeterminate' : undefined"
        :style="{ '--wmp-progress-value': ratio ?? 0 }"
    >
        <div data-part="progress-track">
            <div data-part="progress-fill"></div>
        </div>
    </div>
</template>
