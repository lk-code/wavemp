// Collections of the API are paged with offset and limit (at most 100 items per page).

export interface PageLike<T> {
    items?: T[] | null
    total?: number | null
}

export const maxPageLimit = 100

/** Loads all pages of a collection, one after the other. */
export async function fetchAllPages<T>(
    fetchPage: (offset: number, limit: number) => Promise<PageLike<T> | undefined>,
    limit = maxPageLimit,
): Promise<T[]> {
    const items: T[] = []
    let offset = 0

    for (;;) {
        const page = await fetchPage(offset, limit)
        const pageItems = page?.items ?? []
        items.push(...pageItems)
        offset += pageItems.length

        // An empty page also ends the loop, so a changing total can never loop forever.
        if (pageItems.length === 0 || offset >= (page?.total ?? 0)) {
            return items
        }
    }
}
