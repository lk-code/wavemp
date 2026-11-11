import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { apiClient } from '@/api/client'
import { ProblemCodeObject, type Scan } from '@/api/generated/models'
import { problemCodeOf, statusOf } from '@/api/problems'
import { activityOf, isActive, type Activity } from '@/admin/presentation'

export type ScansStatus = 'idle' | 'loading' | 'ready' | 'error'
export type ScanStartResult = 'started' | 'alreadyRunning' | 'error'

/** Scans shown per page of the history. */
export const historyPageSize = 10

/** Scans of the media folder for the admin page: the current one and the history. */
export const useScansStore = defineStore('scans', () => {
    const current = ref<Scan | null>(null)
    const history = ref<Scan[]>([])
    const historyTotal = ref(0)
    const status = ref<ScansStatus>('idle')

    const activity = computed<Activity>(() => activityOf(current.value?.status))
    /** The newest finished scan of the loaded history. */
    const lastFinished = computed(
        () => history.value.find((scan) => !isActive(scan.status)) ?? null,
    )
    const hasMoreHistory = computed(() => history.value.length < historyTotal.value)

    /** Loads the queued or running scan; null if there is none. */
    async function loadCurrent(): Promise<Scan | null> {
        try {
            current.value = (await apiClient.api.v1.scans.current.get()) ?? null
        } catch (error) {
            if (
                problemCodeOf(error) !== ProblemCodeObject.ScanNot_running &&
                statusOf(error) !== 404
            ) {
                throw error
            }

            current.value = null
        }

        return current.value
    }

    /** Loads the first page of the history again, or the next page. */
    async function loadHistory(options: { reset?: boolean } = {}): Promise<void> {
        const reset = options.reset ?? true
        status.value = 'loading'
        try {
            const page = await apiClient.api.v1.scans.get({
                queryParameters: {
                    offset: reset ? 0 : history.value.length,
                    limit: historyPageSize,
                },
            })
            const items = page?.items ?? []
            history.value = reset ? items : [...history.value, ...items]
            historyTotal.value = page?.total ?? history.value.length
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    /** Starts a scan of the whole media folder. */
    async function start(): Promise<ScanStartResult> {
        try {
            current.value = (await apiClient.api.v1.scans.post()) ?? null
            return 'started'
        } catch (error) {
            if (problemCodeOf(error) === ProblemCodeObject.ScanAlready_running) {
                await loadCurrent().catch(() => null)
                return 'alreadyRunning'
            }

            return 'error'
        }
    }

    return {
        current,
        history,
        historyTotal,
        status,
        activity,
        lastFinished,
        hasMoreHistory,
        loadCurrent,
        loadHistory,
        start,
    }
})
