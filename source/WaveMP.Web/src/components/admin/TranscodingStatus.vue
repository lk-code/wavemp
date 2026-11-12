<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { TranscodingStatus } from '@/api/generated/models'
import { acceleratorKey, hardwareReasonKey, hardwareStatePresentation } from '@/admin/transcoding'
import BaseAlert from '@/components/base/BaseAlert.vue'
import { useFormat } from '@/composables/useFormat'

/** Whether the server can transcode, the state of the hardware acceleration and the load. */
const props = defineProps<{
    status: TranscodingStatus
}>()

const { t } = useI18n()
const format = useFormat()

const hardware = computed(() => hardwareStatePresentation(props.status.hardware))
const reasonKey = computed(() => hardwareReasonKey(props.status.hardware?.reason))

function yesNo(value: boolean | null | undefined): string {
    return value ? t('adminTranscoding.yes') : t('adminTranscoding.no')
}

const statistics = computed(() => {
    const items = [
        {
            id: 'ffmpeg',
            labelKey: 'adminTranscoding.stats.ffmpeg',
            text: yesNo(props.status.ffmpegAvailable),
        },
        {
            id: 'cache',
            labelKey: 'adminTranscoding.stats.cache',
            text: yesNo(props.status.cacheAvailable),
        },
        {
            id: 'running-jobs',
            labelKey: 'adminTranscoding.stats.jobs',
            text: t('adminTranscoding.jobCount', {
                running: format.number(props.status.runningJobs ?? 0),
                max: format.number(props.status.maxParallelJobs ?? 0),
            }),
        },
        {
            id: 'encoder',
            labelKey: 'adminTranscoding.stats.encoder',
            text: t(acceleratorKey(props.status.hardware?.accelerator)),
        },
    ]
    const device = props.status.hardware?.device
    if (device) {
        items.push({ id: 'device', labelKey: 'adminTranscoding.stats.device', text: device })
    }

    return items
})
</script>

<template>
    <div data-part="transcoding-status">
        <BaseAlert
            v-if="!status.available"
            tone="error"
            :message="t('adminTranscoding.unavailable')"
        />
        <dl data-part="stat-list" :aria-label="t('adminTranscoding.stats.label')">
            <div data-part="stat-item" data-item="hardware">
                <dt data-part="label">{{ t('adminTranscoding.stats.hardware') }}</dt>
                <dd data-part="stat-value">
                    <span data-part="badge" :data-state="hardware.token">
                        <span data-part="label">{{ t(hardware.labelKey) }}</span>
                    </span>
                </dd>
            </div>
            <div
                v-for="item in statistics"
                :key="item.id"
                data-part="stat-item"
                :data-item="item.id"
            >
                <dt data-part="label">{{ t(item.labelKey) }}</dt>
                <dd data-part="stat-value">{{ item.text }}</dd>
            </div>
        </dl>
        <span v-if="reasonKey" data-part="label">{{ t(reasonKey) }}</span>
        <span data-part="label">{{ t('adminTranscoding.hint') }}</span>
    </div>
</template>
