// The search text in the toolbar and in the query of the route (?q=).

import type { LocationQuery, RouteLocationRaw } from 'vue-router'

/** Longest search text the server accepts. */
export const maxQueryLength = 200

/** The text as it is searched: trimmed, white space joined, at most 200 characters. */
export function normalizeQuery(text: string | null | undefined): string {
    return (text ?? '').trim().replace(/\s+/g, ' ').slice(0, maxQueryLength).trim()
}

/** The search text of the query of a route; empty without one. */
export function queryText(query: LocationQuery): string {
    const value = Array.isArray(query.q) ? query.q[0] : query.q
    return normalizeQuery(typeof value === 'string' ? value : '')
}

/** The search page for a text; without text the page shows its hint. */
export function searchLocation(text: string): RouteLocationRaw {
    const q = normalizeQuery(text)
    return { name: 'library-search', query: q ? { q } : {} }
}
