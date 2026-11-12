import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { createPinia, setActivePinia } from 'pinia'

const api = vi.hoisted(() => ({
    listFavorites: vi.fn(),
    addFavorite: vi.fn(),
    removeFavorite: vi.fn(),
    setTrackRating: vi.fn(),
    getHome: vi.fn(),
}))

vi.mock('@/collection/api', () => api)

const { useCollectionStore } = await import('../collection')
const { useSessionStore } = await import('../session')

function deferred() {
    let resolve: () => void = () => {}
    let reject: (error: Error) => void = () => {}
    const promise = new Promise<void>((done, fail) => {
        resolve = done
        reject = fail
    })
    return { promise, resolve, reject }
}

describe('collection store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
    })

    it('shows the state of the API until the profile changes it', () => {
        const store = useCollectionStore()

        expect(store.isFavorite('Track', 't1', true)).toBe(true)
        expect(store.isFavorite('Track', 't1', null)).toBe(false)
        expect(store.ratingOf('t1', 4)).toBe(4)
        expect(store.ratingOf('t1', undefined)).toBe(0)
    })

    it('marks a favorite at once and keeps it once the API saved it', async () => {
        const request = deferred()
        api.addFavorite.mockReturnValue(request.promise)
        const store = useCollectionStore()

        const saving = store.toggleFavorite('Album', 'a1', false)

        expect(store.isFavorite('Album', 'a1', false)).toBe(true)
        expect(store.isPending('Album', 'a1')).toBe(true)
        expect(api.addFavorite).toHaveBeenCalledWith('Album', 'a1')
        request.resolve()
        expect(await saving).toBe(true)
        expect(store.isPending('Album', 'a1')).toBe(false)
        expect(store.revision).toBe(1)
        // The same item in another type stays untouched.
        expect(store.isFavorite('Artist', 'a1', false)).toBe(false)
    })

    it('unmarks a favorite of the API', async () => {
        api.removeFavorite.mockResolvedValue(undefined)
        const store = useCollectionStore()

        expect(await store.toggleFavorite('Track', 't1', true)).toBe(true)

        expect(api.removeFavorite).toHaveBeenCalledWith('Track', 't1')
        expect(store.isFavorite('Track', 't1', true)).toBe(false)
    })

    it('takes a change back and reports the error if the API rejects it', async () => {
        api.addFavorite.mockRejectedValue(new Error('offline'))
        api.setTrackRating
            .mockResolvedValueOnce(undefined)
            .mockRejectedValueOnce(new Error('offline'))
        const store = useCollectionStore()

        expect(await store.toggleFavorite('Track', 't1', false)).toBe(false)
        expect(store.isFavorite('Track', 't1', false)).toBe(false)
        expect(store.error).toBe('favorite')

        expect(await store.rate('t1', 4)).toBe(true)
        expect(store.error).toBeNull()
        expect(await store.rate('t1', 2)).toBe(false)
        expect(store.ratingOf('t1', 0)).toBe(4)
        expect(store.error).toBe('rating')

        store.clearError()
        expect(store.error).toBeNull()
    })

    it('ignores clicks while the same change is being saved', async () => {
        const request = deferred()
        api.addFavorite.mockReturnValue(request.promise)
        const store = useCollectionStore()

        const first = store.toggleFavorite('Track', 't1', false)
        expect(await store.toggleFavorite('Track', 't1', false)).toBe(false)

        request.resolve()
        await first
        expect(api.addFavorite).toHaveBeenCalledTimes(1)
        expect(store.isFavorite('Track', 't1', false)).toBe(true)
    })

    it('rates with 0 to 5 stars only', async () => {
        api.setTrackRating.mockResolvedValue(undefined)
        const store = useCollectionStore()

        expect(await store.rate('t1', 6)).toBe(false)
        expect(await store.rate('t1', 1.5)).toBe(false)
        expect(api.setTrackRating).not.toHaveBeenCalled()

        expect(await store.rate('t1', 0)).toBe(true)
        expect(api.setTrackRating).toHaveBeenCalledWith('t1', 0)
        expect(store.ratingOf('t1', 3)).toBe(0)
    })

    it('forgets the changes when the profile changes', async () => {
        api.addFavorite.mockResolvedValue(undefined)
        const store = useCollectionStore()
        const session = useSessionStore()
        await store.toggleFavorite('Track', 't1', false)

        session.$patch({ token: 'other' })
        await nextTick()

        expect(store.isFavorite('Track', 't1', false)).toBe(false)
    })
})
