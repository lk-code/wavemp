import { beforeEach, describe, expect, it } from 'vitest'
import {
    clearStoredSession,
    loadStoredSession,
    saveStoredSession,
    sessionStorageKey,
    type StoredSession,
} from '../storage'

const now = new Date('2026-11-11T10:00:00Z')

const session: StoredSession = {
    token: 'token',
    sessionId: '0193a5b2-0000-7000-8000-000000000001',
    expiresAt: '2027-11-11T10:00:00.000Z',
    profile: {
        id: '0193a5b2-0000-7000-8000-000000000002',
        name: 'Anna',
        initials: 'A',
        color: '#1f6fd6',
        role: 'User',
        isBuiltIn: false,
    },
    language: 'de',
}

class MemoryStorage implements Storage {
    private values = new Map<string, string>()

    get length(): number {
        return this.values.size
    }

    clear(): void {
        this.values.clear()
    }

    getItem(key: string): string | null {
        return this.values.get(key) ?? null
    }

    key(index: number): string | null {
        return [...this.values.keys()][index] ?? null
    }

    removeItem(key: string): void {
        this.values.delete(key)
    }

    setItem(key: string, value: string): void {
        this.values.set(key, value)
    }
}

class FailingStorage extends MemoryStorage {
    override getItem(): string | null {
        throw new DOMException('denied', 'SecurityError')
    }

    override setItem(): void {
        throw new DOMException('full', 'QuotaExceededError')
    }

    override removeItem(): void {
        throw new DOMException('denied', 'SecurityError')
    }
}

describe('session storage', () => {
    let storage: MemoryStorage

    beforeEach(() => {
        storage = new MemoryStorage()
    })

    it('saves, loads and clears a session', () => {
        saveStoredSession(session, storage)

        expect(storage.getItem(sessionStorageKey)).not.toBeNull()
        expect(loadStoredSession(now, storage)).toEqual(session)

        clearStoredSession(storage)

        expect(loadStoredSession(now, storage)).toBeNull()
    })

    it('uses a versioned key', () => {
        expect(sessionStorageKey).toBe('wavemp.session.v1')
    })

    it('returns null without a stored session', () => {
        expect(loadStoredSession(now, storage)).toBeNull()
        expect(loadStoredSession(now, null)).toBeNull()
    })

    it('forgets invalid JSON', () => {
        storage.setItem(sessionStorageKey, '{not json')

        expect(loadStoredSession(now, storage)).toBeNull()
    })

    it('forgets sessions with missing or invalid fields', () => {
        const invalid = [
            { ...session, token: '' },
            { ...session, expiresAt: 'tomorrow' },
            { ...session, profile: { ...session.profile, role: 'Owner' } },
            { ...session, profile: { ...session.profile, isBuiltIn: 'no' } },
            { ...session, profile: undefined },
        ]

        for (const value of invalid) {
            storage.setItem(sessionStorageKey, JSON.stringify(value))
            expect(loadStoredSession(now, storage)).toBeNull()
            expect(storage.getItem(sessionStorageKey)).toBeNull()
        }
    })

    it('forgets expired sessions', () => {
        saveStoredSession({ ...session, expiresAt: '2026-11-11T09:59:59.000Z' }, storage)

        expect(loadStoredSession(now, storage)).toBeNull()
        expect(storage.getItem(sessionStorageKey)).toBeNull()
    })

    it('keeps the session but drops an unknown language', () => {
        storage.setItem(sessionStorageKey, JSON.stringify({ ...session, language: 'fr' }))

        expect(loadStoredSession(now, storage)?.language).toBeNull()
    })

    it('survives a storage that throws', () => {
        const failing = new FailingStorage()

        expect(() => saveStoredSession(session, failing)).not.toThrow()
        expect(loadStoredSession(now, failing)).toBeNull()
        expect(() => clearStoredSession(failing)).not.toThrow()
    })
})
