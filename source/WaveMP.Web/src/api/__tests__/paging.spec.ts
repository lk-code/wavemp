import { describe, expect, it, vi } from 'vitest'
import { fetchAllPages, maxPageLimit } from '../paging'

describe('fetchAllPages', () => {
    it('loads every page of the collection', async () => {
        const all = Array.from({ length: 7 }, (_, index) => index)
        const fetchPage = vi.fn(async (offset: number, limit: number) => ({
            items: all.slice(offset, offset + limit),
            total: all.length,
        }))

        const items = await fetchAllPages(fetchPage, 3)

        expect(items).toEqual(all)
        expect(fetchPage.mock.calls).toEqual([
            [0, 3],
            [3, 3],
            [6, 3],
        ])
    })

    it('uses the largest page size by default', async () => {
        const fetchPage = vi.fn(async () => ({ items: [1], total: 1 }))

        await fetchAllPages(fetchPage)

        expect(fetchPage).toHaveBeenCalledWith(0, maxPageLimit)
        expect(maxPageLimit).toBe(100)
    })

    it('stops at an empty page even if the total says more', async () => {
        const fetchPage = vi.fn(async (offset: number) => ({
            items: offset === 0 ? ['a'] : [],
            total: 5,
        }))

        expect(await fetchAllPages(fetchPage, 1)).toEqual(['a'])
        expect(fetchPage).toHaveBeenCalledTimes(2)
    })

    it('treats a missing answer as empty', async () => {
        expect(await fetchAllPages(async () => undefined)).toEqual([])
        expect(await fetchAllPages(async () => ({ items: null, total: null }))).toEqual([])
    })
})
