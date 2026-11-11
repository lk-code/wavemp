import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import { ProblemCodeObject, type HealthStatus, type ProblemDetails } from '@/api/generated/models'

/** Health as shown in the UI; "Unreachable" means the service did not answer as expected. */
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
            // A failed health check arrives as Problem Details with a machine-readable code.
            const code = (error as Partial<ProblemDetails>).code
            status.value = code === ProblemCodeObject.HealthUnhealthy ? 'Unhealthy' : 'Unreachable'
            version.value = null
        }
    }

    return { status, version, check }
})
