import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
    list: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    byId: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                users: {
                    get: (config: unknown) => api.list(config),
                    post: (body: unknown) => api.create(body),
                    byId: (id: string) => {
                        api.byId(id)
                        return {
                            patch: (body: unknown) => api.update(body),
                            delete: () => api.remove(),
                        }
                    },
                },
            },
        },
    },
}))

const { useUsersStore } = await import('../users')

describe('users store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('loads all users sorted by name', async () => {
        api.list.mockResolvedValue({ items: [{ id: 'a' }, { id: 'b' }], total: 2 })
        const store = useUsersStore()

        await store.load()

        expect(store.status).toBe('ready')
        expect(store.users).toEqual([{ id: 'a' }, { id: 'b' }])
        expect(api.list).toHaveBeenCalledWith({
            queryParameters: { offset: 0, limit: 100, sort: 'Name' },
        })
    })

    it('reports errors while loading', async () => {
        api.list.mockRejectedValue({ responseStatusCode: 403, code: 'auth.forbidden' })
        const store = useUsersStore()

        await store.load()

        expect(store.status).toBe('error')
    })

    it('creates, updates and deletes users', async () => {
        api.create.mockResolvedValue({ id: 'new' })
        api.update.mockResolvedValue({ id: 'a', name: 'Renamed' })
        const store = useUsersStore()

        expect(await store.create({ name: 'New' })).toEqual({ id: 'new' })
        expect(await store.update('a', { name: 'Renamed' })).toEqual({ id: 'a', name: 'Renamed' })
        await store.remove('a')

        expect(api.create).toHaveBeenCalledWith({ name: 'New' })
        expect(api.update).toHaveBeenCalledWith({ name: 'Renamed' })
        expect(api.remove).toHaveBeenCalledOnce()
        expect(api.byId.mock.calls).toEqual([['a'], ['a']])
    })

    it('passes errors of changes on', async () => {
        api.create.mockRejectedValue({ code: 'user.name_taken', responseStatusCode: 409 })
        const store = useUsersStore()

        await expect(store.create({ name: 'Taken' })).rejects.toMatchObject({
            code: 'user.name_taken',
        })
    })
})
