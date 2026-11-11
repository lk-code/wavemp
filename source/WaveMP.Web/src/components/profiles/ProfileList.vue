<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import ProfileTile, { type ProfileTileData } from './ProfileTile.vue'
import { profileTileStates } from '@/profiles/presentation'

/** The profiles of the profile selection as tiles. */
defineProps<{
    profiles: readonly ProfileTileData[]
    /** Id of the profile of the current session, if any. */
    currentId?: string | null
    disabled?: boolean
}>()

const emit = defineEmits<{
    select: [profile: ProfileTileData]
}>()

const { t } = useI18n()
</script>

<template>
    <ul data-part="profile-list" :aria-label="t('profiles.list')">
        <li v-for="profile in profiles" :key="profile.id" data-part="profile-item">
            <ProfileTile
                :profile="profile"
                :state="profileTileStates(profile, profiles, currentId)"
                :disabled="disabled"
                @select="emit('select', profile)"
            />
        </li>
    </ul>
</template>
