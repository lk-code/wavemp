import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import type { Profile } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'

export type ProfilesStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Profiles for the profile selection; available without a session. */
export const useProfilesStore = defineStore('profiles', () => {
    const profiles = ref<Profile[]>([])
    const status = ref<ProfilesStatus>('idle')

    async function load(): Promise<void> {
        status.value = 'loading'
        try {
            profiles.value = await fetchAllPages((offset, limit) =>
                apiClient.api.v1.profiles.get({ queryParameters: { offset, limit } }),
            )
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    return { profiles, status, load }
})
