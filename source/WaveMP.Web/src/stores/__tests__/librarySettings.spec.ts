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
                    library: {
                        get: () => api.get(),
                        patch: (body: unknown) => api.patch(body),
                    },
                },
            },
        },
    },
}))

const { useLibrarySettingsStore } = await import('../librarySettings')

describe('library settings store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        api.get.mockReset()
        api.patch.mockReset()
    })

    it('loads the settings', async () => {
        api.get.mockResolvedValue({ scanIntervalMinutes: 360 })
        const store = useLibrarySettingsStore()

        await store.load()

        expect(store.status).toBe('ready')
        expect(store.settings).toEqual({ scanIntervalMinutes: 360 })
    })

    it('reports errors while loading', async () => {
        api.get.mockRejectedValue(new Error('offline'))
        const store = useLibrarySettingsStore()

        await store.load()

        expect(store.status).toBe('error')
    })

    it('saves changes and keeps the saved settings', async () => {
        api.patch.mockResolvedValue({ scanIntervalMinutes: 30 })
        const store = useLibrarySettingsStore()

        const saved = await store.update({ scanIntervalMinutes: 30 })

        expect(saved).toEqual({ scanIntervalMinutes: 30 })
        expect(store.settings).toEqual({ scanIntervalMinutes: 30 })
        expect(api.patch).toHaveBeenCalledWith({ scanIntervalMinutes: 30 })
    })

    it('passes errors of changes on', async () => {
        api.patch.mockRejectedValue({ responseStatusCode: 400, code: 'request.invalid' })
        const store = useLibrarySettingsStore()

        await expect(store.update({ scanIntervalMinutes: 5 })).rejects.toMatchObject({
            code: 'request.invalid',
        })
    })
})
