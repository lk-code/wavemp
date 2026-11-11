<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ProblemCodeObject, RoleObject, type User } from '@/api/generated/models'
import { problemCodeOf } from '@/api/problems'
import UserDeleteDialog from '@/components/admin/UserDeleteDialog.vue'
import UserEditDialog from '@/components/admin/UserEditDialog.vue'
import UserList, { type UserListItem } from '@/components/admin/UserList.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import { useSessionStore } from '@/stores/session'
import { useUsersStore } from '@/stores/users'

/** Admin page: create, edit and delete profiles. */
const { t } = useI18n()
const router = useRouter()
const session = useSessionStore()
const usersStore = useUsersStore()

const editOpen = ref(false)
const editing = ref<UserListItem | null>(null)
const deleteOpen = ref(false)
const deleting = ref<UserListItem | null>(null)
const deleteBusy = ref(false)
const message = ref<string | null>(null)

function toItem(user: User): UserListItem | null {
    if (!user.id || !user.name || !user.initials || !user.color) {
        return null
    }

    return {
        id: user.id,
        name: user.name,
        initials: user.initials,
        color: user.color,
        isAdmin: user.role === RoleObject.Admin,
        isBuiltIn: !!user.isBuiltIn,
        createdAt: user.createdAt ?? null,
    }
}

const items = computed(() => usersStore.users.map(toItem).filter((item) => item !== null))

function openCreate(): void {
    message.value = null
    editing.value = null
    editOpen.value = true
}

function openEdit(user: UserListItem): void {
    message.value = null
    editing.value = user
    editOpen.value = true
}

function openDelete(user: UserListItem): void {
    message.value = null
    deleting.value = user
    deleteOpen.value = true
}

async function onSaved(user: User): Promise<void> {
    await usersStore.load()
    // Name, initials, color or role of the own profile changed.
    if (user.id === session.profile?.id) {
        await session.refresh()
        if (!session.isAdmin) {
            await router.replace({ name: 'library' })
        }
    }
}

async function confirmDelete(): Promise<void> {
    const user = deleting.value
    if (!user || deleteBusy.value) {
        return
    }

    deleteBusy.value = true
    try {
        await usersStore.remove(user.id)
        deleteOpen.value = false
        if (user.id === session.profile?.id) {
            // The own profile is gone together with its session.
            await router.replace({ name: 'profiles' })
            session.clear()
            return
        }
    } catch (failure) {
        deleteOpen.value = false
        switch (problemCodeOf(failure)) {
            case ProblemCodeObject.UserNot_found:
                message.value = t('adminUsers.errors.notFound')
                break
            case ProblemCodeObject.UserBuilt_in_admin:
                message.value = t('adminUsers.builtInNotDeletable')
                break
            default:
                message.value = t('adminUsers.errors.deleteFailed')
        }
    } finally {
        deleteBusy.value = false
    }

    await usersStore.load()
}

onMounted(() => {
    void usersStore.load()
})
</script>

<template>
    <div data-part="view" data-view="admin-users">
        <div data-part="view-actions">
            <BaseButton
                variant="primary"
                icon="add"
                :label="t('adminUsers.add')"
                @click="openCreate"
            />
        </div>
        <BaseAlert
            v-if="usersStore.status === 'error'"
            tone="error"
            :message="t('adminUsers.loadError')"
        />
        <BaseAlert v-if="message" tone="error" :message="message" />
        <UserList :users="items" @edit="openEdit" @delete="openDelete" />
        <UserEditDialog v-model:open="editOpen" :user="editing" @saved="onSaved" />
        <UserDeleteDialog
            v-model:open="deleteOpen"
            :user="deleting"
            :busy="deleteBusy"
            @confirm="confirmDelete"
        />
    </div>
</template>
