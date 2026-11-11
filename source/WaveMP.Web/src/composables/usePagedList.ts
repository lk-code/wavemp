import { computed, ref, shallowRef } from 'vue'
import type { PageLike } from '@/api/paging'

export type PagedListStatus = 'idle' | 'loading' | 'ready' | 'error'

/** Items loaded per page; the most the server returns. */
export const libraryPageSize = 100

/**
 * A list that loads page by page: reload() loads the first page again, loadMore() appends the
 * next one. Answers that arrive after a newer reload are dropped, so changing filters quickly
 * never shows an outdated list.
 */
export function usePagedList<T>(
    fetchPage: (offset: number, limit: number) => Promise<PageLike<T> | undefined>,
    pageSize = libraryPageSize,
) {
    const items = shallowRef<T[]>([])
    const total = ref(0)
    const status = ref<PagedListStatus>('idle')
    const loadingMore = ref(false)
    let generation = 0

    const hasMore = computed(() => status.value === 'ready' && items.value.length < total.value)

    async function reload(): Promise<void> {
        const current = ++generation
        status.value = 'loading'
        loadingMore.value = false
        try {
            const page = await fetchPage(0, pageSize)
            if (current !== generation) {
                return
            }

            items.value = page?.items ?? []
            total.value = page?.total ?? items.value.length
            status.value = 'ready'
        } catch {
            if (current === generation) {
                status.value = 'error'
            }
        }
    }

    async function loadMore(): Promise<void> {
        if (!hasMore.value || loadingMore.value) {
            return
        }

        const current = generation
        loadingMore.value = true
        try {
            const page = await fetchPage(items.value.length, pageSize)
            if (current !== generation) {
                return
            }

            const more = page?.items ?? []
            items.value = [...items.value, ...more]
            // An empty page ends the list, also if the total changed in the meantime.
            total.value = more.length === 0 ? items.value.length : (page?.total ?? total.value)
        } catch {
            // The list stays as it is; the next attempt loads the same page again.
        } finally {
            if (current === generation) {
                loadingMore.value = false
            }
        }
    }

    return { items, total, status, loadingMore, hasMore, reload, loadMore }
}
