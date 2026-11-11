import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

const get = vi.fn()

vi.mock('@/api/client', () => ({
    apiClient: { api: { v1: { health: { get: () => get() } } } },
}))

const { useHealthStore } = await import('../health')

describe('health store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        get.mockReset()
    })

    it('starts with the checking state', () => {
        const health = useHealthStore()

        expect(health.status).toBe('Checking')
        expect(health.version).toBeNull()
    })

    it('reports a healthy service with its version', async () => {
        get.mockResolvedValue({ status: 'Healthy', version: '1.2.3' })
        const health = useHealthStore()

        await health.check()

        expect(health.status).toBe('Healthy')
        expect(health.version).toBe('1.2.3')
    })

    it('reports an unhealthy service for the health.unhealthy problem code', async () => {
        get.mockRejectedValue({ code: 'health.unhealthy' })
        const health = useHealthStore()

        await health.check()

        expect(health.status).toBe('Unhealthy')
        expect(health.version).toBeNull()
    })

    it('reports an unreachable service for any other error', async () => {
        get.mockRejectedValue(new TypeError('Failed to fetch'))
        const health = useHealthStore()

        await health.check()

        expect(health.status).toBe('Unreachable')
    })

    it('reports an unreachable service for an empty answer', async () => {
        get.mockResolvedValue(undefined)
        const health = useHealthStore()

        await health.check()

        expect(health.status).toBe('Unreachable')
    })
})
