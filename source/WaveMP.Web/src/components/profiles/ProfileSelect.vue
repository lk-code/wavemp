<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import ProfileList from './ProfileList.vue'
import type { ProfileTileData } from './ProfileTile.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import { isFirstStart } from '@/profiles/presentation'

export type ProfileSelectState = 'loading' | 'ready' | 'error'

/**
 * The profile selection page without data access: the view passes the profiles of the server,
 * the component overview samples.
 */
const props = withDefaults(
    defineProps<{
        profiles: readonly ProfileTileData[]
        state: ProfileSelectState
        currentId?: string | null
        /** Message about a failed selection. */
        error?: string | null
        busy?: boolean
    }>(),
    { currentId: null, error: null, busy: false },
)

const emit = defineEmits<{
    select: [profile: ProfileTileData]
    retry: []
}>()

const { t } = useI18n()

const firstStart = computed(() => props.state === 'ready' && isFirstStart(props.profiles))
</script>

<template>
    <main data-part="profile-select" :data-state="state" :aria-busy="busy ? 'true' : undefined">
        <span data-part="logo">
            <span data-part="logo-image" aria-hidden="true"></span>
            <span data-part="label">{{ t('app.name') }}</span>
        </span>
        <h1 data-part="profile-select-title">{{ t('profiles.title') }}</h1>
        <BaseAlert v-if="error" tone="error" :message="error" />
        <div v-if="state === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('profiles.loadErrorTitle') }}</p>
            <p data-part="empty-state-text">{{ t('profiles.loadErrorText') }}</p>
            <BaseButton :label="t('profiles.retry')" @click="emit('retry')" />
        </div>
        <ProfileList
            v-else
            :profiles="profiles"
            :current-id="currentId"
            :disabled="busy"
            @select="emit('select', $event)"
        />
        <p v-if="firstStart" data-part="profile-select-hint" role="note">
            {{ t('profiles.firstStartHint') }}
        </p>
    </main>
</template>
