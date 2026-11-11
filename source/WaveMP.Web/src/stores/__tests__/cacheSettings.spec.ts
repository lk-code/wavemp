import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
    get: vi.fn(),
    patch: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                settings: {
                    cache: {
                        get: () => api.get(),
                        patch: (body: unknown) => api.patch(body),
                    },
                },
            },
        },
    },
}))

const { useCacheSettingsStore } = await import('../cacheSettings')

describe('cache settings store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        api.get.mockReset()
        api.patch.mockReset()
    })

    it('loads the settings', async () => {
        api.get.mockResolvedValue({ maxSizeMegabytes: 2000 })
        const store = useCacheSettingsStore()

        await store.load()

        expect(store.status).toBe('ready')
        expect(store.settings).toEqual({ maxSizeMegabytes: 2000 })
    })

    it('reports errors while loading', async () => {
        api.get.mockRejectedValue(new Error('offline'))
        const store = useCacheSettingsStore()

        await store.load()

        expect(store.status).toBe('error')
    })

    it('saves changes and keeps the saved settings', async () => {
        api.patch.mockResolvedValue({ maxSizeMegabytes: 5000 })
        const store = useCacheSettingsStore()

        const saved = await store.update({ maxSizeMegabytes: 5000 })

        expect(saved).toEqual({ maxSizeMegabytes: 5000 })
        expect(store.settings).toEqual({ maxSizeMegabytes: 5000 })
        expect(api.patch).toHaveBeenCalledWith({ maxSizeMegabytes: 5000 })
    })

    it('passes errors of changes on', async () => {
        api.patch.mockRejectedValue({ responseStatusCode: 400, code: 'request.invalid' })
        const store = useCacheSettingsStore()

        await expect(store.update({ maxSizeMegabytes: 5 })).rejects.toMatchObject({
            code: 'request.invalid',
        })
    })
})
