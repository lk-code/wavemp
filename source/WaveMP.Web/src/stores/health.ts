import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import type { HealthStatus } from '@/api/generated/models'

/** Health as shown in the UI; "Unreachable" means the service did not answer at all. */
export type DisplayHealthStatus = HealthStatus | 'Checking' | 'Unreachable'

export const useHealthStore = defineStore('health', () => {
    const status = ref<DisplayHealthStatus>('Checking')
    const version = ref<string | null>(null)

    async function check(): Promise<void> {
        status.value = 'Checking'
        try {
            const health = await apiClient.api.v1.health.get()
            status.value = health?.status ?? 'Unreachable'
            version.value = health?.version ?? null
        } catch (error) {
            // A 503 arrives as Problem Details (with a status code); anything else means no answer.
            const responseStatusCode = (error as { responseStatusCode?: number }).responseStatusCode
            status.value = responseStatusCode === 503 ? 'Unhealthy' : 'Unreachable'
            version.value = null
        }
    }

    return { status, version, check }
})
