import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { apiClient } from '@/api/client'
import { ProblemCodeObject, type Job } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'
import { problemCodeOf } from '@/api/problems'

export type JobsStatus = 'idle' | 'loading' | 'ready' | 'error'
export type JobRunResult = 'started' | 'alreadyRunning' | 'error'

/** Background jobs for the admin page. */
export const useJobsStore = defineStore('jobs', () => {
    const jobs = ref<Job[]>([])
    const status = ref<JobsStatus>('idle')

    const anyActive = computed(() => jobs.value.some((job) => job.state && job.state !== 'Idle'))

    async function load(): Promise<void> {
        if (jobs.value.length === 0) {
            status.value = 'loading'
        }

        try {
            jobs.value = await fetchAllPages((offset, limit) =>
                apiClient.api.v1.jobs.get({ queryParameters: { offset, limit } }),
            )
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    /** Starts a run of the job now. */
    async function run(id: string): Promise<JobRunResult> {
        try {
            await apiClient.api.v1.jobs.byId(id).runs.post()
            return 'started'
        } catch (error) {
            return problemCodeOf(error) === ProblemCodeObject.JobAlready_running
                ? 'alreadyRunning'
                : 'error'
        }
    }

    return { jobs, status, anyActive, load, run }
})
