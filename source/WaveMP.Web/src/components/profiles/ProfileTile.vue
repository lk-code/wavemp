<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import ProfileAvatar from './ProfileAvatar.vue'

export interface ProfileTileData {
    readonly id: string
    readonly name: string
    readonly initials: string
    readonly color: string
    readonly isBuiltIn: boolean
}

/** Tile of the profile selection: avatar and name; selecting it selects the profile. */
defineProps<{
    profile: ProfileTileData
    /** data-state tokens: built-in, secondary, current. */
    state?: string
    disabled?: boolean
}>()

const emit = defineEmits<{
    select: []
}>()

const { t } = useI18n()
</script>

<template>
    <button
        type="button"
        data-part="profile-tile"
        :data-state="state"
        :title="profile.isBuiltIn ? t('profiles.builtIn') : undefined"
        :aria-current="state?.split(' ').includes('current') ? 'true' : undefined"
        :disabled="disabled"
        @click="emit('select')"
    >
        <ProfileAvatar :initials="profile.initials" :color="profile.color" />
        <span data-part="label">{{ profile.name }}</span>
    </button>
</template>
