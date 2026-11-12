<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { EqualizerPreset } from '@/api/generated/models'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import BaseSlider from '@/components/base/BaseSlider.vue'
import { useFormat } from '@/composables/useFormat'
import {
    equalizerPresets,
    equalizerStepDb,
    isEqualizerPreset,
    type EqualizerBandLayout,
} from '@/player/equalizer'

/**
 * The equalizer: on and off, preset, reset, the preamp and one vertical slider per band. It is
 * controlled through props and events only; the equalizer store saves the changes.
 */
const props = withDefaults(
    defineProps<{
        enabled: boolean
        preset: EqualizerPreset
        preampDb: number
        /** The curve that applies, one value per band. */
        gainsDb: readonly number[]
        bands: readonly EqualizerBandLayout[]
        minGainDb: number
        maxGainDb: number
        /** False if this browser cannot apply the equalizer (no Web Audio). */
        available?: boolean
    }>(),
    { available: true },
)

const emit = defineEmits<{
    enable: [enabled: boolean]
    preset: [preset: EqualizerPreset]
    band: [index: number, db: number]
    preamp: [db: number]
    reset: []
}>()

const { t } = useI18n()
const format = useFormat()

/** A large step of the sliders (Page Up and Page Down). */
const largeStepDb = 3

const presetLabels = computed<Record<EqualizerPreset, string>>(() => ({
    Flat: t('equalizer.presets.flat'),
    Rock: t('equalizer.presets.rock'),
    Pop: t('equalizer.presets.pop'),
    Jazz: t('equalizer.presets.jazz'),
    Classical: t('equalizer.presets.classical'),
    Dance: t('equalizer.presets.dance'),
    Bass: t('equalizer.presets.bass'),
    Treble: t('equalizer.presets.treble'),
    Vocal: t('equalizer.presets.vocal'),
    Custom: t('equalizer.presets.custom'),
}))

const presetOptions = computed<SelectOption[]>(() =>
    equalizerPresets.map((preset) => ({ value: preset, label: presetLabels.value[preset] })),
)

const selectedPreset = computed({
    get: () => props.preset,
    set: (value: string) => {
        if (isEqualizerPreset(value)) {
            emit('preset', value)
        }
    },
})

function frequencyText(hertz: number): string {
    const frequency = format.frequency(hertz)
    return frequency.unit === 'hertz'
        ? t('equalizer.hertz', { value: frequency.value })
        : t('equalizer.kilohertz', { value: frequency.value })
}

function gainText(db: number): string {
    return t('equalizer.gain', { value: format.decibels(db) })
}

const scale = computed(() =>
    [props.maxGainDb, 0, props.minGainDb].map((db) =>
        t('equalizer.gain', { value: format.number(db, { signDisplay: 'exceptZero' }) }),
    ),
)

const bandItems = computed(() =>
    props.bands.map((band, index) => ({
        index,
        frequency: band.frequency,
        label: frequencyText(band.frequency),
        gain: props.gainsDb[index] ?? 0,
    })),
)
</script>

<template>
    <section
        data-part="equalizer"
        :data-state="enabled ? 'on' : 'off'"
        :aria-label="t('equalizer.title')"
    >
        <div data-part="equalizer-controls" role="group" :aria-label="t('equalizer.controls')">
            <BaseButton
                data-item="power"
                icon="equalizer"
                :label="t('equalizer.power')"
                :pressed="enabled"
                :state="enabled ? 'on' : 'off'"
                @click="emit('enable', !enabled)"
            />
            <BaseSelect
                v-model="selectedPreset"
                data-field="equalizer-preset"
                :label="t('equalizer.preset')"
                :options="presetOptions"
            />
            <BaseButton
                data-item="reset"
                icon="reset"
                :label="t('equalizer.reset')"
                @click="emit('reset')"
            />
        </div>
        <BaseAlert v-if="!available" tone="info" :message="t('equalizer.unavailable')" />
        <div data-part="equalizer-bands" role="group" :aria-label="t('equalizer.bands')">
            <div data-part="equalizer-band" data-kind="preamp">
                <span data-part="equalizer-value" aria-hidden="true">{{
                    format.decibels(preampDb)
                }}</span>
                <BaseSlider
                    orientation="vertical"
                    :model-value="preampDb"
                    :min="minGainDb"
                    :max="maxGainDb"
                    :step="equalizerStepDb"
                    :large-step="largeStepDb"
                    :label="t('equalizer.preamp')"
                    :value-text="gainText(preampDb)"
                    @update:model-value="emit('preamp', $event)"
                />
                <span data-part="label" aria-hidden="true">{{ t('equalizer.preamp') }}</span>
            </div>
            <div data-part="equalizer-scale" aria-hidden="true">
                <span v-for="(text, index) in scale" :key="index" data-part="label">{{
                    text
                }}</span>
            </div>
            <div
                v-for="band in bandItems"
                :key="band.frequency"
                data-part="equalizer-band"
                data-kind="band"
                :data-frequency="band.frequency"
            >
                <span data-part="equalizer-value" aria-hidden="true">{{
                    format.decibels(band.gain)
                }}</span>
                <BaseSlider
                    orientation="vertical"
                    :model-value="band.gain"
                    :min="minGainDb"
                    :max="maxGainDb"
                    :step="equalizerStepDb"
                    :large-step="largeStepDb"
                    :label="band.label"
                    :value-text="gainText(band.gain)"
                    @update:model-value="emit('band', band.index, $event)"
                />
                <span data-part="label" aria-hidden="true">{{ band.label }}</span>
            </div>
        </div>
    </section>
</template>
