import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
    list: vi.fn(),
    run: vi.fn(),
    byId: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                jobs: {
                    get: (config: unknown) => api.list(config),
                    byId: (id: string) => {
                        api.byId(id)
                        return { runs: { post: () => api.run() } }
                    },
                },
            },
        },
    },
}))

const { useJobsStore } = await import('../jobs')

describe('jobs store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('loads all jobs and knows whether one is active', async () => {
        api.list.mockResolvedValue({ items: [{ id: 'library-scan', state: 'Running' }], total: 1 })
        const store = useJobsStore()

        await store.load()

        expect(store.status).toBe('ready')
        expect(store.jobs).toEqual([{ id: 'library-scan', state: 'Running' }])
        expect(store.anyActive).toBe(true)
        expect(api.list).toHaveBeenCalledWith({ queryParameters: { offset: 0, limit: 100 } })
    })

    it('reports errors while loading', async () => {
        api.list.mockRejectedValue(new Error('offline'))
        const store = useJobsStore()

        await store.load()

        expect(store.status).toBe('error')
    })

    it('runs a job and reports a running one', async () => {
        const store = useJobsStore()
        api.run.mockResolvedValueOnce({ id: 'run' })
        api.run.mockRejectedValueOnce({ responseStatusCode: 409, code: 'job.already_running' })
        api.run.mockRejectedValueOnce({ responseStatusCode: 404, code: 'job.not_found' })

        expect(await store.run('library-scan')).toBe('started')
        expect(await store.run('library-scan')).toBe('alreadyRunning')
        expect(await store.run('unknown')).toBe('error')
        expect(api.byId.mock.calls).toEqual([['library-scan'], ['library-scan'], ['unknown']])
    })
})
