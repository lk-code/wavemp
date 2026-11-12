<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { TranscodeJob } from '@/api/generated/models'
import { acceleratorKey, segmentRatio } from '@/admin/transcoding'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseProgress from '@/components/base/BaseProgress.vue'
import { useFormat } from '@/composables/useFormat'

/** The running transcodes with their song, progress and a button that stops them. */
withDefaults(
    defineProps<{
        jobs: readonly TranscodeJob[]
        /** Id of the transcode being stopped. */
        cancelling?: string | null
    }>(),
    { cancelling: null },
)

const emit = defineEmits<{
    cancel: [id: string]
}>()

const { t } = useI18n()
const format = useFormat()

function title(job: TranscodeJob): string {
    const name = job.track?.title ?? job.trackId ?? ''
    const artist = job.track?.artist?.name
    return artist ? t('adminTranscoding.jobTitle', { title: name, artist }) : name
}

function mediaKey(job: TranscodeJob): string {
    return job.media === 'Video' ? 'adminTranscoding.media.video' : 'adminTranscoding.media.audio'
}

function segments(job: TranscodeJob): string {
    return t('adminTranscoding.segments', {
        done: format.number(job.completedSegments ?? 0),
        total: format.number(job.segmentCount ?? 0),
    })
}

function startedAt(job: TranscodeJob): string {
    return job.startedAt
        ? t('adminTranscoding.startedAt', {
              time: format.date(job.startedAt, { timeStyle: 'medium' }),
          })
        : ''
}
</script>

<template>
    <div v-if="jobs.length === 0" data-part="empty-state">
        <p data-part="empty-state-text">{{ t('adminTranscoding.noJobs') }}</p>
    </div>
    <ul v-else data-part="transcode-list" :aria-label="t('adminTranscoding.jobs')">
        <li
            v-for="job in jobs"
            :key="job.id ?? ''"
            data-part="transcode-item"
            :data-state="job.media === 'Video' ? 'video' : 'audio'"
        >
            <div data-part="transcode-info">
                <span data-part="transcode-title">{{ title(job) }}</span>
                <span data-part="transcode-meta">
                    <span data-part="badge" :data-state="job.media === 'Video' ? 'video' : 'audio'">
                        <span data-part="label">{{ t(mediaKey(job)) }}</span>
                    </span>
                    <span data-part="label">{{ t(acceleratorKey(job.accelerator)) }}</span>
                    <span data-part="label">{{ segments(job) }}</span>
                    <span v-if="job.startedAt" data-part="label">{{ startedAt(job) }}</span>
                </span>
                <BaseProgress
                    :label="t('adminTranscoding.progress')"
                    :value="segmentRatio(job)"
                    :value-text="segments(job)"
                />
            </div>
            <div data-part="transcode-actions">
                <BaseButton
                    icon="close"
                    data-item="cancel"
                    :label="t('adminTranscoding.cancel')"
                    :disabled="cancelling === job.id"
                    @click="emit('cancel', job.id ?? '')"
                />
            </div>
        </li>
    </ul>
</template>
