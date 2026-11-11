import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
    get: vi.fn(),
    delete: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                cache: {
                    get: () => api.get(),
                    delete: () => api.delete(),
                },
            },
        },
    },
}))

const { useCacheStore } = await import('../cache')

const usage = {
    isAvailable: true,
    sizeBytes: 1000,
    entryCount: 2,
    maxSizeBytes: 2_000_000_000,
    covers: { sizeBytes: 1000, entryCount: 2 },
    transcodes: { sizeBytes: 0, entryCount: 0 },
}

describe('cache store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        api.get.mockReset()
        api.delete.mockReset()
    })

    it('loads the usage of the cache', async () => {
        api.get.mockResolvedValue(usage)
        const store = useCacheStore()

        await store.load()

        expect(store.status).toBe('ready')
        expect(store.usage).toEqual(usage)
    })

    it('reports errors while loading', async () => {
        api.get.mockRejectedValue(new Error('offline'))
        const store = useCacheStore()

        await store.load()

        expect(store.status).toBe('error')
        expect(store.usage).toBeNull()
    })

    it('clears the cache and loads the usage again', async () => {
        api.delete.mockResolvedValue(undefined)
        api.get.mockResolvedValue({ ...usage, sizeBytes: 0, entryCount: 0 })
        const store = useCacheStore()

        await store.clear()

        expect(api.delete).toHaveBeenCalledTimes(1)
        expect(store.usage?.entryCount).toBe(0)
    })

    it('passes errors of clearing on', async () => {
        api.delete.mockRejectedValue({ responseStatusCode: 500, code: 'server.error' })
        const store = useCacheStore()

        await expect(store.clear()).rejects.toMatchObject({ code: 'server.error' })
        expect(api.get).not.toHaveBeenCalled()
    })
})
