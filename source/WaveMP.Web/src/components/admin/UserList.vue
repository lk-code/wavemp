<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import ProfileAvatar from '@/components/profiles/ProfileAvatar.vue'
import { useFormat } from '@/composables/useFormat'

export interface UserListItem {
    readonly id: string
    readonly name: string
    readonly initials: string
    readonly color: string
    readonly isAdmin: boolean
    readonly isBuiltIn: boolean
    readonly createdAt: Date | null
}

/** Profiles on the admin page with their role and actions. */
defineProps<{
    users: readonly UserListItem[]
}>()

const emit = defineEmits<{
    edit: [user: UserListItem]
    delete: [user: UserListItem]
}>()

const { t } = useI18n()
const format = useFormat()
</script>

<template>
    <ul data-part="user-list" :aria-label="t('adminUsers.list')">
        <li
            v-for="user in users"
            :key="user.id"
            data-part="user-item"
            :data-state="user.isBuiltIn ? 'built-in' : undefined"
        >
            <ProfileAvatar :initials="user.initials" :color="user.color" />
            <div data-part="user-info">
                <span data-part="user-name">{{ user.name }}</span>
                <span data-part="user-meta">
                    <span data-part="badge" :data-state="user.isAdmin ? 'admin' : 'user'">
                        <span data-part="label">{{
                            user.isAdmin ? t('adminUsers.roles.admin') : t('adminUsers.roles.user')
                        }}</span>
                    </span>
                    <span v-if="user.isBuiltIn" data-part="badge" data-state="built-in">
                        <span data-part="label">{{ t('adminUsers.builtIn') }}</span>
                    </span>
                    <span v-if="user.createdAt" data-part="label">{{
                        t('adminUsers.created', { date: format.date(user.createdAt) })
                    }}</span>
                </span>
            </div>
            <div data-part="user-actions">
                <BaseButton
                    variant="icon"
                    icon="edit"
                    :label="t('adminUsers.edit', { name: user.name })"
                    @click="emit('edit', user)"
                />
                <BaseButton
                    variant="icon"
                    icon="delete"
                    :label="
                        user.isBuiltIn
                            ? t('adminUsers.builtInNotDeletable')
                            : t('adminUsers.delete', { name: user.name })
                    "
                    :disabled="user.isBuiltIn"
                    @click="emit('delete', user)"
                />
            </div>
        </li>
    </ul>
</template>
