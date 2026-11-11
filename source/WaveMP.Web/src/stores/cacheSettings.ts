import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import type { CacheSettings, UpdateCacheSettings } from '@/api/generated/models'

export type CacheSettingsStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Settings of the cache. Changes pass errors on to the page, which shows them. */
export const useCacheSettingsStore = defineStore('cacheSettings', () => {
    const settings = ref<CacheSettings | null>(null)
    const status = ref<CacheSettingsStatus>('idle')

    async function load(): Promise<void> {
        status.value = 'loading'
        try {
            settings.value = (await apiClient.api.v1.settings.cache.get()) ?? null
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    async function update(changes: UpdateCacheSettings): Promise<CacheSettings | null> {
        const saved = (await apiClient.api.v1.settings.cache.patch(changes)) ?? null
        if (saved) {
            settings.value = saved
        }

        return saved
    }

    return { settings, status, load, update }
})
