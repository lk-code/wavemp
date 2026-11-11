<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ProblemCodeObject } from '@/api/generated/models'
import { problemCodeOf } from '@/api/problems'
import ProfileSelect from '@/components/profiles/ProfileSelect.vue'
import type { ProfileTileData } from '@/components/profiles/ProfileTile.vue'
import { safeRedirect } from '@/router/guards'
import { useProfilesStore } from '@/stores/profiles'
import { useSessionStore } from '@/stores/session'
import { toStoredProfile } from '@/session/storage'

/** Profile selection: shown on start without a selected profile and after switching profiles. */
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const profilesStore = useProfilesStore()
const session = useSessionStore()

const busy = ref(false)
const error = ref<string | null>(null)

const profiles = computed<ProfileTileData[]>(() =>
    profilesStore.profiles.map(toStoredProfile).filter((profile) => profile !== null),
)
const state = computed(() => {
    switch (profilesStore.status) {
        case 'ready':
            return 'ready'
        case 'error':
            return 'error'
        default:
            return 'loading'
    }
})
const currentId = computed(() => (session.isAuthenticated ? session.profile?.id : null))

async function choose(profile: ProfileTileData): Promise<void> {
    if (busy.value) {
        return
    }

    busy.value = true
    error.value = null
    try {
        await session.select(profile.id)
        await router.replace(safeRedirect(route.query.redirect))
    } catch (failure) {
        if (problemCodeOf(failure) === ProblemCodeObject.ProfileNot_found) {
            error.value = t('profiles.profileGone')
            void profilesStore.load()
        } else {
            error.value = t('profiles.selectError')
        }
    } finally {
        busy.value = false
    }
}

onMounted(() => {
    void profilesStore.load()
})
</script>

<template>
    <ProfileSelect
        :profiles="profiles"
        :state="state"
        :current-id="currentId"
        :error="error"
        :busy="busy"
        @select="choose"
        @retry="profilesStore.load()"
    />
</template>
