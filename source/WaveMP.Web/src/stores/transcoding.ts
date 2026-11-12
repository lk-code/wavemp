import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import type {
    TranscodeJob,
    TranscodingSettings,
    TranscodingStatus,
    UpdateTranscodingSettings,
} from '@/api/generated/models'

export type TranscodingLoadStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Running transcodes the admin page shows at most. */
export const transcodeJobLimit = 50

/**
 * Transcoding for the admin page: its status with the hardware acceleration, the running
 * transcodes and the settings. Cancelling and saving pass errors on to the page, which shows them.
 */
export const useTranscodingStore = defineStore('transcoding', () => {
    const status = ref<TranscodingStatus | null>(null)
    const jobs = ref<TranscodeJob[]>([])
    const loadStatus = ref<TranscodingLoadStatus>('idle')
    const settings = ref<TranscodingSettings | null>(null)
    const settingsStatus = ref<TranscodingLoadStatus>('idle')

    /** Loads status and running transcodes. */
    async function load(): Promise<void> {
        if (!status.value) {
            loadStatus.value = 'loading'
        }

        try {
            const transcoding = apiClient.api.v1.transcoding
            const [current, page] = await Promise.all([
                transcoding.status.get(),
                transcoding.jobs.get({ queryParameters: { limit: transcodeJobLimit } }),
            ])
            status.value = current ?? null
            jobs.value = page?.items ?? []
            loadStatus.value = 'ready'
        } catch {
            loadStatus.value = 'error'
        }
    }

    /** Stops a running transcode, then loads the list again. */
    async function cancel(jobId: string): Promise<void> {
        await apiClient.api.v1.transcoding.jobs.byJobId(jobId).delete()
        await load()
    }

    async function loadSettings(): Promise<void> {
        settingsStatus.value = 'loading'
        try {
            settings.value = (await apiClient.api.v1.settings.transcoding.get()) ?? null
            settingsStatus.value = 'ready'
        } catch {
            settingsStatus.value = 'error'
        }
    }

    async function updateSettings(
        changes: UpdateTranscodingSettings,
    ): Promise<TranscodingSettings | null> {
        const saved = (await apiClient.api.v1.settings.transcoding.patch(changes)) ?? null
        if (saved) {
            settings.value = saved
        }

        return saved
    }

    return {
        status,
        jobs,
        loadStatus,
        settings,
        settingsStatus,
        load,
        cancel,
        loadSettings,
        updateSettings,
    }
})
