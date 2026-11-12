// Access to the pages: without a selected profile, only public pages (the profile selection) are
// reachable; admin pages need a profile with the role admin.

import type { RouteLocationNormalized, RouteLocationRaw } from 'vue-router'

export interface AccessState {
    readonly isAuthenticated: boolean
    readonly isAdmin: boolean
}

export type AccessTarget = Pick<RouteLocationNormalized, 'fullPath' | 'meta' | 'redirectedFrom'>

/** Where a navigation may go: true, or the page to go to instead. */
export function resolveAccess(to: AccessTarget, state: AccessState): true | RouteLocationRaw {
    if (to.meta.public) {
        return true
    }

    if (!state.isAuthenticated) {
        // The start page needs no way back; every other page is opened after the selection.
        const requested = to.redirectedFrom?.fullPath ?? to.fullPath
        return { name: 'profiles', query: requested === '/' ? {} : { redirect: to.fullPath } }
    }

    if (to.meta.admin && !state.isAdmin) {
        return { name: 'home' }
    }

    return true
}

/**
 * The page to open after selecting a profile: the requested path if it is a path within the app
 * (no other origin, not the profile selection itself), otherwise the start page.
 */
export function safeRedirect(value: unknown): string {
    if (
        typeof value !== 'string' ||
        !value.startsWith('/') ||
        value.startsWith('//') ||
        value.startsWith('/\\') ||
        /^\/profiles(?:[/?#]|$)/.test(value)
    ) {
        return '/'
    }

    return value
}
