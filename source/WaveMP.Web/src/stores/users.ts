import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiClient } from '@/api/client'
import { UserSortObject, type CreateUser, type UpdateUser, type User } from '@/api/generated/models'
import { fetchAllPages } from '@/api/paging'

export type UsersStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Profiles for the admin page. Changes pass errors on to the page, which shows them. */
export const useUsersStore = defineStore('users', () => {
    const users = ref<User[]>([])
    const status = ref<UsersStatus>('idle')

    async function load(): Promise<void> {
        status.value = 'loading'
        try {
            users.value = await fetchAllPages((offset, limit) =>
                apiClient.api.v1.users.get({
                    queryParameters: { offset, limit, sort: UserSortObject.Name },
                }),
            )
            status.value = 'ready'
        } catch {
            status.value = 'error'
        }
    }

    async function create(input: CreateUser): Promise<User | undefined> {
        return apiClient.api.v1.users.post(input)
    }

    async function update(id: string, input: UpdateUser): Promise<User | undefined> {
        return apiClient.api.v1.users.byId(id).patch(input)
    }

    async function remove(id: string): Promise<void> {
        await apiClient.api.v1.users.byId(id).delete()
    }

    return { users, status, load, create, update, remove }
})
