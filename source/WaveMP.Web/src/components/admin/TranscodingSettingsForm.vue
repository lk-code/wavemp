<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
    HardwareAccelerationModeObject,
    type HardwareAccelerationMode,
    type TranscodingSettings,
} from '@/api/generated/models'
import { problemCodeOf, validationErrorsOf } from '@/api/problems'
import {
    audioBitrateOptions,
    maxParallelJobOptions,
    maxVideoHeightOptions,
} from '@/admin/transcoding'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import { useFormat } from '@/composables/useFormat'
import { validationMessagesByField } from '@/i18n/validation'
import { useTranscodingStore } from '@/stores/transcoding'

type Field = 'hardwareAcceleration' | 'maxParallelJobs' | 'maxVideoHeight' | 'audioBitrate'

/** Settings of the transcoding: hardware acceleration, parallel transcodes and the quality. */
const emit = defineEmits<{
    saved: []
}>()

const { t } = useI18n()
const format = useFormat()
const store = useTranscodingStore()

const hardware = ref<string>(HardwareAccelerationModeObject.Auto)
const maxParallelJobs = ref('')
const maxVideoHeight = ref('')
const audioBitrate = ref('')
const saving = ref(false)
const saved = ref(false)
const failed = ref(false)
const errors = ref<Partial<Record<Field, string>>>({})

function show(settings: TranscodingSettings | null): void {
    if (!settings) {
        return
    }

    hardware.value = settings.hardwareAcceleration ?? HardwareAccelerationModeObject.Auto
    maxParallelJobs.value = String(settings.maxParallelJobs ?? '')
    maxVideoHeight.value = String(settings.maxVideoHeight ?? '')
    audioBitrate.value = String(settings.audioBitrateKbps ?? '')
}

/** The offered values, plus the current one if the server holds another. */
function optionsOf(values: readonly number[], current: string, label: (value: number) => string) {
    const offered: SelectOption[] = values.map((value) => ({
        value: String(value),
        label: label(value),
    }))
    if (current && !offered.some((option) => option.value === current)) {
        offered.push({ value: current, label: label(Number(current)) })
    }

    return offered
}

const hardwareOptions = computed<SelectOption[]>(() => [
    { value: HardwareAccelerationModeObject.Auto, label: t('adminTranscoding.hardwareModes.auto') },
    { value: HardwareAccelerationModeObject.Off, label: t('adminTranscoding.hardwareModes.off') },
])
const parallelOptions = computed(() =>
    optionsOf(maxParallelJobOptions, maxParallelJobs.value, (value) => format.number(value)),
)
const heightOptions = computed(() =>
    optionsOf(maxVideoHeightOptions, maxVideoHeight.value, (value) =>
        t('adminTranscoding.height', { height: format.number(value) }),
    ),
)
const bitrateOptions = computed(() =>
    optionsOf(audioBitrateOptions, audioBitrate.value, (value) =>
        t('adminTranscoding.bitrate', { bitrate: format.number(value) }),
    ),
)

async function save(): Promise<void> {
    if (saving.value) {
        return
    }

    saving.value = true
    saved.value = false
    failed.value = false
    errors.value = {}
    try {
        show(
            await store.updateSettings({
                hardwareAcceleration: hardware.value as HardwareAccelerationMode,
                maxParallelJobs: Number(maxParallelJobs.value),
                maxVideoHeight: Number(maxVideoHeight.value),
                audioBitrateKbps: Number(audioBitrate.value),
            }),
        )
        saved.value = true
        emit('saved')
    } catch (error) {
        const messages = validationMessagesByField<Field>(
            validationErrorsOf(error),
            {
                hardwareAcceleration: 'hardwareAcceleration',
                maxParallelJobs: 'maxParallelJobs',
                maxVideoHeight: 'maxVideoHeight',
                audioBitrateKbps: 'audioBitrate',
            },
            (key, parameters) => t(key, parameters),
        )
        if (problemCodeOf(error) === 'request.invalid' && Object.keys(messages).length > 0) {
            errors.value = messages
        } else {
            failed.value = true
        }
    } finally {
        saving.value = false
    }
}

watch(() => store.settings, show)

onMounted(async () => {
    if (!store.settings) {
        await store.loadSettings()
    }

    show(store.settings)
})
</script>

<template>
    <BaseAlert
        v-if="store.settingsStatus === 'error' && !store.settings"
        tone="error"
        :message="t('adminTranscoding.settingsLoadError')"
    />
    <form v-else data-part="form" @submit.prevent="save">
        <BaseSelect
            v-model="hardware"
            :label="t('adminTranscoding.hardwareMode')"
            :options="hardwareOptions"
            :hint="t('adminTranscoding.hardwareModeHint')"
            :error="errors.hardwareAcceleration"
            :disabled="saving"
        />
        <BaseSelect
            v-model="maxParallelJobs"
            :label="t('adminTranscoding.maxParallelJobs')"
            :options="parallelOptions"
            :hint="t('adminTranscoding.maxParallelJobsHint')"
            :error="errors.maxParallelJobs"
            :disabled="saving"
        />
        <BaseSelect
            v-model="maxVideoHeight"
            :label="t('adminTranscoding.maxVideoHeight')"
            :options="heightOptions"
            :hint="t('adminTranscoding.maxVideoHeightHint')"
            :error="errors.maxVideoHeight"
            :disabled="saving"
        />
        <BaseSelect
            v-model="audioBitrate"
            :label="t('adminTranscoding.audioBitrate')"
            :options="bitrateOptions"
            :hint="t('adminTranscoding.audioBitrateHint')"
            :error="errors.audioBitrate"
            :disabled="saving"
        />
        <BaseAlert v-if="saved" tone="success" :message="t('adminTranscoding.saved')" />
        <BaseAlert v-if="failed" tone="error" :message="t('adminTranscoding.saveError')" />
        <div data-part="view-actions">
            <BaseButton
                type="submit"
                variant="primary"
                :label="t('adminTranscoding.save')"
                :disabled="saving || !store.settings"
            />
        </div>
    </form>
</template>
