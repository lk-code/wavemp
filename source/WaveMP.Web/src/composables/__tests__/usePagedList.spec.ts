import { describe, expect, it, vi } from 'vitest'
import { usePagedList } from '../usePagedList'

function page(items: string[], total: number) {
    return { items, total }
}

describe('usePagedList', () => {
    it('loads the first page and then the next ones', async () => {
        const fetchPage = vi
            .fn()
            .mockResolvedValueOnce(page(['a', 'b'], 3))
            .mockResolvedValueOnce(page(['c'], 3))
        const list = usePagedList<string>(fetchPage, 2)

        await list.reload()
        const firstHasMore = list.hasMore.value
        await list.loadMore()

        expect(firstHasMore).toBe(true)
        expect(list.items.value).toEqual(['a', 'b', 'c'])
        expect(list.status.value).toBe('ready')
        expect(list.hasMore.value).toBe(false)
        expect(fetchPage.mock.calls).toEqual([
            [0, 2],
            [2, 2],
        ])
    })

    it('drops the answer of an older reload', async () => {
        let resolveFirst: (value: unknown) => void = () => {}
        const fetchPage = vi
            .fn()
            .mockReturnValueOnce(new Promise((resolve) => (resolveFirst = resolve)))
            .mockResolvedValueOnce(page(['new'], 1))
        const list = usePagedList<string>(fetchPage)

        const first = list.reload()
        await list.reload()
        resolveFirst(page(['old'], 1))
        await first

        expect(list.items.value).toEqual(['new'])
    })

    it('reports errors of the first page and keeps the list on errors of further pages', async () => {
        const failing = usePagedList<string>(vi.fn().mockRejectedValue(new Error('offline')))
        await failing.reload()
        expect(failing.status.value).toBe('error')

        const fetchPage = vi
            .fn()
            .mockResolvedValueOnce(page(['a'], 2))
            .mockRejectedValueOnce(new Error('offline'))
        const list = usePagedList<string>(fetchPage, 1)
        await list.reload()
        await list.loadMore()

        expect(list.items.value).toEqual(['a'])
        expect(list.loadingMore.value).toBe(false)
        expect(list.hasMore.value).toBe(true)
    })

    it('ends the list at an empty page', async () => {
        const fetchPage = vi
            .fn()
            .mockResolvedValueOnce(page(['a'], 5))
            .mockResolvedValueOnce(page([], 5))
        const list = usePagedList<string>(fetchPage, 1)

        await list.reload()
        await list.loadMore()

        expect(list.total.value).toBe(1)
        expect(list.hasMore.value).toBe(false)
    })
})
