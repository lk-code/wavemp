<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { Job } from '@/api/generated/models'
import {
    activityOf,
    durationSeconds,
    errorLabelKey,
    jobNameKey,
    statusPresentation,
} from '@/admin/presentation'
import BaseButton from '@/components/base/BaseButton.vue'
import { useFormat } from '@/composables/useFormat'

/** Background jobs with their last and next run and a button to run them now. */
withDefaults(
    defineProps<{
        jobs: readonly Job[]
        /** Id of the job whose start is being requested. */
        starting?: string | null
    }>(),
    { starting: null },
)

const emit = defineEmits<{
    run: [id: string]
}>()

const { t } = useI18n()
const format = useFormat()

function name(job: Job): string {
    const key = jobNameKey(job.id)
    return key ? t(key) : (job.id ?? '')
}

function state(job: Job): string {
    return activityOf(job.state === 'Idle' ? null : job.state)
}

function dateTime(date: Date): string {
    return format.date(date, { dateStyle: 'medium', timeStyle: 'short' })
}
</script>

<template>
    <ul data-part="job-list" :aria-label="t('adminJobs.list')">
        <li
            v-for="job in jobs"
            :key="job.id ?? ''"
            data-part="job-item"
            :data-item="job.id"
            :data-state="state(job)"
        >
            <div data-part="job-info">
                <span data-part="job-name">{{ name(job) }}</span>
                <span data-part="job-meta">
                    <span
                        v-if="job.lastRun"
                        data-part="badge"
                        :data-state="statusPresentation(job.lastRun.status).token"
                    >
                        <span data-part="label">{{
                            t(statusPresentation(job.lastRun.status).labelKey)
                        }}</span>
                    </span>
                    <span
                        v-if="job.lastRun?.startedAt ?? job.lastRun?.queuedAt"
                        data-part="label"
                        >{{
                            t('adminJobs.lastRun', {
                                date: dateTime((job.lastRun.startedAt ?? job.lastRun.queuedAt)!),
                            })
                        }}</span
                    >
                    <span v-else data-part="label">{{ t('adminJobs.never') }}</span>
                    <span
                        v-if="durationSeconds(job.lastRun?.durationMilliseconds) !== null"
                        data-part="label"
                        >{{
                            t('adminJobs.duration', {
                                duration: format.duration(
                                    durationSeconds(job.lastRun?.durationMilliseconds) ?? 0,
                                ),
                            })
                        }}</span
                    >
                    <span v-if="job.nextRunAt" data-part="label">{{
                        t('adminJobs.nextRun', { date: dateTime(job.nextRunAt) })
                    }}</span>
                    <span v-if="job.intervalMinutes" data-part="label">{{
                        t('adminJobs.schedule', { minutes: format.number(job.intervalMinutes) })
                    }}</span>
                    <span v-else data-part="label">{{ t('adminJobs.manualOnly') }}</span>
                </span>
                <span v-if="errorLabelKey(job.lastRun?.errorCode)" data-part="label">{{
                    t(errorLabelKey(job.lastRun?.errorCode) ?? '')
                }}</span>
            </div>
            <div data-part="job-actions">
                <BaseButton
                    icon="run-job"
                    :label="t('adminJobs.runNow')"
                    :disabled="state(job) !== 'idle' || starting === job.id"
                    @click="emit('run', job.id ?? '')"
                />
            </div>
        </li>
    </ul>
</template>
