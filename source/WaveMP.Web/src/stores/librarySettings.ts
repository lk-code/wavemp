import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import type { LibrarySettings, UpdateLibrarySettings } from '@/api/generated/models'

export type LibrarySettingsStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Settings of the media library. Changes pass errors on to the page, which shows them. */
export const useLibrarySettingsStore = defineStore('librarySettings', () => {
    const settings = ref<LibrarySettings | null>(null)
    const status = ref<LibrarySettingsStatus>('idle')

    async function load(): Promise<void> {
        status.value = 'loading'
        try {
            settings.value = (await apiClient.api.v1.settings.library.get()) ?? null
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    async function update(changes: UpdateLibrarySettings): Promise<LibrarySettings | null> {
        const saved = (await apiClient.api.v1.settings.library.patch(changes)) ?? null
        if (saved) {
            settings.value = saved
        }

        return saved
    }

    return { settings, status, load, update }
})
