import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import type { CacheUsage } from '@/api/generated/models'

export type CacheStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Usage of the cache for the admin page. Clearing passes errors on to the page, which shows them. */
export const useCacheStore = defineStore('cache', () => {
    const usage = ref<CacheUsage | null>(null)
    const status = ref<CacheStatus>('idle')

    async function load(): Promise<void> {
        if (!usage.value) {
            status.value = 'loading'
        }

        try {
            usage.value = (await apiClient.api.v1.cache.get()) ?? null
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    /** Removes all files of the cache, then loads the usage again. */
    async function clear(): Promise<void> {
        await apiClient.api.v1.cache.delete()
        await load()
    }

    return { usage, status, load, clear }
})
