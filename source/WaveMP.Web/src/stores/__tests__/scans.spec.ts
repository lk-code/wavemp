import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import type { Scan } from '@/api/generated/models'

const api = vi.hoisted(() => ({
    list: vi.fn(),
    start: vi.fn(),
    current: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                scans: {
                    get: (config: unknown) => api.list(config),
                    post: () => api.start(),
                    current: { get: () => api.current() },
                },
            },
        },
    },
}))

const { useScansStore } = await import('../scans')

const running: Scan = { id: 'run', status: 'Running', progress: { phase: 'Discovering' } }
const finished: Scan = { id: 'done', status: 'Succeeded' }

describe('scans store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('loads the running scan and its activity', async () => {
        api.current.mockResolvedValue(running)
        const store = useScansStore()

        await store.loadCurrent()

        expect(store.current).toEqual(running)
        expect(store.activity).toBe('running')
    })

    it('treats scan.not_running as no active scan', async () => {
        api.current.mockRejectedValue({ responseStatusCode: 404, code: 'scan.not_running' })
        const store = useScansStore()
        store.current = running

        expect(await store.loadCurrent()).toBeNull()
        expect(store.activity).toBe('idle')
    })

    it('passes other errors of the current scan on', async () => {
        api.current.mockRejectedValue({ responseStatusCode: 500, code: 'server.error' })
        const store = useScansStore()

        await expect(store.loadCurrent()).rejects.toMatchObject({ code: 'server.error' })
    })

    it('loads the history page by page and finds the last finished scan', async () => {
        api.list
            .mockResolvedValueOnce({ items: [running, finished], total: 3 })
            .mockResolvedValueOnce({ items: [{ id: 'older', status: 'Failed' }], total: 3 })
        const store = useScansStore()

        await store.loadHistory()
        const firstPage = store.hasMoreHistory
        await store.loadHistory({ reset: false })

        expect(firstPage).toBe(true)
        expect(store.history.map((scan) => scan.id)).toEqual(['run', 'done', 'older'])
        expect(store.lastFinished?.id).toBe('done')
        expect(store.hasMoreHistory).toBe(false)
        expect(api.list.mock.calls.map((call) => call[0])).toEqual([
            { queryParameters: { offset: 0, limit: 10 } },
            { queryParameters: { offset: 2, limit: 10 } },
        ])
    })

    it('reports errors while loading the history', async () => {
        api.list.mockRejectedValue(new Error('offline'))
        const store = useScansStore()

        await store.loadHistory()

        expect(store.status).toBe('error')
    })

    it('starts a scan and reports a running one', async () => {
        api.start.mockResolvedValueOnce({ id: 'new', status: 'Queued' })
        const store = useScansStore()

        expect(await store.start()).toBe('started')
        expect(store.activity).toBe('queued')

        api.start.mockRejectedValueOnce({ responseStatusCode: 409, code: 'scan.already_running' })
        api.current.mockResolvedValueOnce(running)
        expect(await store.start()).toBe('alreadyRunning')
        expect(store.current).toEqual(running)

        api.start.mockRejectedValueOnce(new Error('offline'))
        expect(await store.start()).toBe('error')
    })
})
