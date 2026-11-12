import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import type { FavoriteType } from '@/api/generated/models'
import { addFavorite, removeFavorite, setTrackRating } from '@/collection/api'
import { isValidRating } from '@/collection/presentation'
import { useSessionStore } from '@/stores/session'

/** What failed last: marking a favorite or rating a track. */
export type CollectionError = 'favorite' | 'rating'

function key(type: FavoriteType | 'Rating', id: string): string {
    return `${type}:${id}`
}

/**
 * Favorites and ratings of the current profile that changed since the lists were loaded. Lists
 * show the state of the API (isFavorite, rating) unless the store knows a newer one, so a heart
 * or a star changed in one place shows everywhere at once. Changes are shown at once and taken
 * back if the API rejects them; the error stays until the next change or clearError.
 */
export const useCollectionStore = defineStore('collection', () => {
    const session = useSessionStore()
    const favorites = ref(new Map<string, boolean>())
    const ratings = ref(new Map<string, number>())
    const pending = ref(new Set<string>())
    const error = ref<CollectionError | null>(null)
    /** Increases with every saved change, e.g. to load the home page again. */
    const revision = ref(0)

    /** Whether the item is a favorite: the latest change or the state of the API. */
    function isFavorite(type: FavoriteType, id: string, fallback?: boolean | null): boolean {
        return favorites.value.get(key(type, id)) ?? fallback ?? false
    }

    /** Stars of a track (0 to 5): the latest change or the state of the API. */
    function ratingOf(id: string, fallback?: number | null): number {
        return ratings.value.get(id) ?? fallback ?? 0
    }

    function isPending(type: FavoriteType | 'Rating', id: string): boolean {
        return pending.value.has(key(type, id))
    }

    async function change(
        pendingKey: string,
        apply: () => void,
        undo: () => void,
        save: () => Promise<void>,
        kind: CollectionError,
    ): Promise<boolean> {
        if (pending.value.has(pendingKey)) {
            return false
        }

        const token = session.token
        error.value = null
        pending.value.add(pendingKey)
        apply()
        try {
            await save()
            revision.value++
            return true
        } catch {
            // A change of the profile in between already forgot the state.
            if (token === session.token) {
                undo()
                error.value = kind
            }
            return false
        } finally {
            pending.value.delete(pendingKey)
        }
    }

    /** Marks or unmarks a favorite; returns whether the API saved it. */
    function setFavorite(type: FavoriteType, id: string, value: boolean): Promise<boolean> {
        const itemKey = key(type, id)
        const previous = favorites.value.get(itemKey)
        return change(
            itemKey,
            () => favorites.value.set(itemKey, value),
            () => restore(favorites.value, itemKey, previous),
            () => (value ? addFavorite(type, id) : removeFavorite(type, id)),
            'favorite',
        )
    }

    function toggleFavorite(
        type: FavoriteType,
        id: string,
        fallback?: boolean | null,
    ): Promise<boolean> {
        return setFavorite(type, id, !isFavorite(type, id, fallback))
    }

    /** Rates a track with 1 to 5 stars, 0 removes the rating; returns whether the API saved it. */
    function rate(id: string, rating: number): Promise<boolean> {
        if (!isValidRating(rating)) {
            return Promise.resolve(false)
        }

        const previous = ratings.value.get(id)
        return change(
            key('Rating', id),
            () => ratings.value.set(id, rating),
            () => restore(ratings.value, id, previous),
            () => setTrackRating(id, rating),
            'rating',
        )
    }

    function clearError(): void {
        error.value = null
    }

    /** Forgets everything, e.g. when the profile changes. */
    function clear(): void {
        favorites.value = new Map()
        ratings.value = new Map()
        pending.value = new Set()
        error.value = null
    }

    // Favorites and ratings belong to the profile of the session.
    watch(
        () => session.token,
        () => clear(),
    )

    return {
        error,
        revision,
        isFavorite,
        ratingOf,
        isPending,
        setFavorite,
        toggleFavorite,
        rate,
        clearError,
        clear,
    }
})

function restore<T>(map: Map<string, T>, itemKey: string, previous: T | undefined): void {
    if (previous === undefined) {
        map.delete(itemKey)
    } else {
        map.set(itemKey, previous)
    }
}
