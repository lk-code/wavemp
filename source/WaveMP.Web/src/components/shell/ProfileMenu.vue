<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { RoleObject, type Role } from '@/api/generated/models'
import BaseContextMenu, { type MenuItem } from '@/components/base/BaseContextMenu.vue'
import ProfileAvatar from '@/components/profiles/ProfileAvatar.vue'
import { useSessionStore } from '@/stores/session'

export interface ProfileMenuProfile {
    readonly name: string
    readonly initials: string
    readonly color: string
    /** Admins also get the profile administration. */
    readonly role?: Role
}

/**
 * Button with the avatar of the current profile; opens the settings, the profile administration
 * (admins only) and the profile switch.
 */
const props = defineProps<{
    profile: ProfileMenuProfile
}>()

const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()

const menu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const button = ref<HTMLButtonElement | null>(null)
const open = computed(() => menu.value?.isOpen ?? false)

const isAdmin = computed(() => props.profile.role === RoleObject.Admin)

const items = computed<MenuItem[]>(() => [
    { id: 'settings', label: t('profileMenu.settings'), icon: 'settings' },
    ...(isAdmin.value
        ? [
              {
                  id: 'manage-profiles',
                  label: t('profileMenu.manageProfiles'),
                  icon: 'manage-profiles' as const,
              },
          ]
        : []),
    { id: 'separator', separator: true },
    { id: 'switch-profile', label: t('profileMenu.switchProfile'), icon: 'switch-profile' },
])

function toggle(): void {
    if (open.value) {
        menu.value?.close()
        return
    }

    const rect = button.value?.getBoundingClientRect()
    void menu.value?.openAt(rect?.left ?? 0, rect?.bottom ?? 0)
}

async function choose(id: string): Promise<void> {
    switch (id) {
        case 'settings':
            await router.push({ name: 'settings' })
            break
        case 'manage-profiles':
            await router.push({ name: 'admin-users' })
            break
        case 'switch-profile':
            // The selection opens first, so the end of the session does not lead back here.
            await router.push({ name: 'profiles' })
            await session.signOut()
            break
    }
}

const title = computed(() => t('profileMenu.open', { name: props.profile.name }))
</script>

<template>
    <button
        ref="button"
        type="button"
        data-part="btn-profile"
        data-variant="icon"
        aria-haspopup="menu"
        :aria-expanded="open ? 'true' : 'false'"
        :title="title"
        @click="toggle"
    >
        <ProfileAvatar :initials="profile.initials" :color="profile.color" />
        <span data-part="label">{{ title }}</span>
    </button>
    <BaseContextMenu ref="menu" :items="items" :label="t('profileMenu.label')" @select="choose" />
</template>
