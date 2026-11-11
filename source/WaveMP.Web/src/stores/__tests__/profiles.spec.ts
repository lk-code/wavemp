import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const get = vi.fn()

vi.mock('@/api/client', () => ({
    apiClient: { api: { v1: { profiles: { get: (config: unknown) => get(config) } } } },
}))

const { useProfilesStore } = await import('../profiles')

describe('profiles store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        get.mockReset()
    })

    it('starts idle without profiles', () => {
        const store = useProfilesStore()

        expect(store.status).toBe('idle')
        expect(store.profiles).toEqual([])
    })

    it('loads all pages of profiles', async () => {
        get.mockImplementation(async ({ queryParameters }) => ({
            items: queryParameters.offset === 0 ? [{ id: 'a' }] : [],
            total: 1,
        }))
        const store = useProfilesStore()

        const loading = store.load()
        expect(store.status).toBe('loading')
        await loading

        expect(store.status).toBe('ready')
        expect(store.profiles).toEqual([{ id: 'a' }])
        expect(get).toHaveBeenCalledWith({ queryParameters: { offset: 0, limit: 100 } })
    })

    it('reports errors', async () => {
        get.mockRejectedValue(new TypeError('Failed to fetch'))
        const store = useProfilesStore()

        await store.load()

        expect(store.status).toBe('error')
    })
})
