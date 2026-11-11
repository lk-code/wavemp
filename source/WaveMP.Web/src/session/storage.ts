// The selected profile is remembered in localStorage, so the next start goes straight on. Stored
// are the token of the session, a snapshot of the profile and its language; the server stays the
// source of truth and refreshes the snapshot on start. Storage can be unavailable (private mode,
// blocked site data), so every access is guarded and invalid or expired data counts as no session.

import { RoleObject, type Role } from '@/api/generated/models'
import { isSupportedLocale, type SupportedLocale } from '@/i18n/locales'

export interface StoredProfile {
    readonly id: string
    readonly name: string
    readonly initials: string
    readonly color: string
    readonly role: Role
    readonly isBuiltIn: boolean
}

export interface StoredSession {
    readonly token: string
    readonly sessionId: string
    /** ISO 8601. */
    readonly expiresAt: string
    readonly profile: StoredProfile
    /** Language of the profile; null while it is unknown. */
    readonly language: SupportedLocale | null
}

/** Key in localStorage; the version changes if the stored format changes. */
export const sessionStorageKey = 'wavemp.session.v1'

const roles = new Set<string>(Object.values(RoleObject))

function defaultStorage(): Storage | null {
    try {
        return window.localStorage
    } catch {
        return null
    }
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null
}

function isNonEmptyString(value: unknown): value is string {
    return typeof value === 'string' && value !== ''
}

/** A profile snapshot with all fields, or null. */
export function toStoredProfile(value: unknown): StoredProfile | null {
    if (
        !isRecord(value) ||
        !isNonEmptyString(value.id) ||
        !isNonEmptyString(value.name) ||
        !isNonEmptyString(value.initials) ||
        !isNonEmptyString(value.color) ||
        typeof value.role !== 'string' ||
        !roles.has(value.role) ||
        typeof value.isBuiltIn !== 'boolean'
    ) {
        return null
    }

    return {
        id: value.id,
        name: value.name,
        initials: value.initials,
        color: value.color,
        role: value.role as Role,
        isBuiltIn: value.isBuiltIn,
    }
}

function toStoredSession(value: unknown, now: Date): StoredSession | null {
    if (
        !isRecord(value) ||
        !isNonEmptyString(value.token) ||
        !isNonEmptyString(value.sessionId) ||
        typeof value.expiresAt !== 'string'
    ) {
        return null
    }

    const expiresAt = Date.parse(value.expiresAt)
    const profile = toStoredProfile(value.profile)
    if (Number.isNaN(expiresAt) || expiresAt <= now.getTime() || !profile) {
        return null
    }

    const language =
        typeof value.language === 'string' && isSupportedLocale(value.language)
            ? value.language
            : null

    return {
        token: value.token,
        sessionId: value.sessionId,
        expiresAt: value.expiresAt,
        profile,
        language,
    }
}

/** The remembered session, or null if there is none or it is invalid or expired. */
export function loadStoredSession(
    now: Date = new Date(),
    storage: Storage | null = defaultStorage(),
): StoredSession | null {
    try {
        const raw = storage?.getItem(sessionStorageKey)
        if (!raw) {
            return null
        }

        const session = toStoredSession(JSON.parse(raw), now)
        if (!session) {
            storage?.removeItem(sessionStorageKey)
        }

        return session
    } catch {
        return null
    }
}

export function saveStoredSession(
    session: StoredSession,
    storage: Storage | null = defaultStorage(),
): void {
    try {
        storage?.setItem(sessionStorageKey, JSON.stringify(session))
    } catch {
        // The session then lasts until the page is closed.
    }
}

export function clearStoredSession(storage: Storage | null = defaultStorage()): void {
    try {
        storage?.removeItem(sessionStorageKey)
    } catch {
        // Nothing to forget.
    }
}
