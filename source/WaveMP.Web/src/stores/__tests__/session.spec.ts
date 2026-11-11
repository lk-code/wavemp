import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { sessionTokens } from '@/api/auth'
import { loadStoredSession, saveStoredSession, type StoredSession } from '@/session/storage'

const api = vi.hoisted(() => ({
    createSession: vi.fn(),
    getCurrentSession: vi.fn(),
    deleteCurrentSession: vi.fn(),
    getSettings: vi.fn(),
    updateSettings: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                sessions: {
                    post: (body: unknown) => api.createSession(body),
                    current: {
                        get: () => api.getCurrentSession(),
                        delete: () => api.deleteCurrentSession(),
                    },
                },
                me: {
                    settings: {
                        get: () => api.getSettings(),
                        patch: (body: unknown) => api.updateSettings(body),
                    },
                },
            },
        },
    },
}))

const { useSessionStore } = await import('../session')

const expiresAt = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000)

function profile(id: string, role: 'Admin' | 'User' = 'User') {
    return { id, name: `Profile ${id}`, initials: 'P', color: '#1f6fd6', role, isBuiltIn: false }
}

function created(id: string, token = `token-${id}`, role: 'Admin' | 'User' = 'User') {
    return {
        token,
        session: { id: `session-${id}`, profile: profile(id, role), expiresAt },
    }
}

function stored(id: string, language: 'en' | 'de' | null = 'de'): StoredSession {
    return {
        token: `token-${id}`,
        sessionId: `session-${id}`,
        expiresAt: expiresAt.toISOString(),
        profile: profile(id),
        language,
    }
}

describe('session store', () => {
    beforeEach(() => {
        localStorage.clear()
        setActivePinia(createPinia())
        sessionTokens.current = null
        sessionTokens.onUnauthorized = null
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
        api.getSettings.mockResolvedValue({ language: 'en' })
    })

    it('restores the remembered session and listens for rejected tokens', () => {
        saveStoredSession(stored('a'))
        const session = useSessionStore()

        session.restore()

        expect(session.isAuthenticated).toBe(true)
        expect(session.profile?.id).toBe('a')
        expect(session.language).toBe('de')
        expect(sessionTokens.current).toBe('token-a')

        sessionTokens.onUnauthorized?.()

        expect(session.isAuthenticated).toBe(false)
        expect(sessionTokens.current).toBeNull()
        expect(loadStoredSession()).toBeNull()
    })

    it('starts without a session if nothing is remembered', () => {
        const session = useSessionStore()

        session.restore()

        expect(session.isAuthenticated).toBe(false)
        expect(session.isAdmin).toBe(false)
    })

    it('selects a profile, loads its language and remembers both', async () => {
        api.createSession.mockResolvedValue(created('a', 'token-a', 'Admin'))
        api.getSettings.mockResolvedValue({ language: 'de' })
        const session = useSessionStore()

        await session.select('a')

        expect(api.createSession).toHaveBeenCalledWith({ profileId: 'a' })
        expect(session.isAuthenticated).toBe(true)
        expect(session.isAdmin).toBe(true)
        expect(session.token).toBe('token-a')
        expect(session.language).toBe('de')
        expect(sessionTokens.current).toBe('token-a')
        expect(loadStoredSession()).toMatchObject({
            token: 'token-a',
            sessionId: 'session-a',
            profile: { id: 'a', role: 'Admin' },
            language: 'de',
        })
    })

    it('keeps the session if the language cannot be loaded', async () => {
        api.createSession.mockResolvedValue(created('a'))
        api.getSettings.mockRejectedValue(new TypeError('Failed to fetch'))
        const session = useSessionStore()

        await session.select('a')

        expect(session.isAuthenticated).toBe(true)
        expect(session.language).toBeNull()
    })

    it('does nothing when the current profile is selected again', async () => {
        saveStoredSession(stored('a'))
        const session = useSessionStore()
        session.restore()

        await session.select('a')

        expect(api.createSession).not.toHaveBeenCalled()
        expect(api.deleteCurrentSession).not.toHaveBeenCalled()
    })

    it('ends the session of another profile before selecting a new one', async () => {
        saveStoredSession(stored('a'))
        api.createSession.mockResolvedValue(created('b'))
        const session = useSessionStore()
        session.restore()

        await session.select('b')

        expect(api.deleteCurrentSession).toHaveBeenCalledOnce()
        expect(session.profile?.id).toBe('b')
        expect(session.token).toBe('token-b')
    })

    it('passes errors of the selection on', async () => {
        api.createSession.mockRejectedValue({ code: 'profile.not_found', responseStatusCode: 404 })
        const session = useSessionStore()

        await expect(session.select('gone')).rejects.toMatchObject({ code: 'profile.not_found' })
        expect(session.isAuthenticated).toBe(false)
    })

    it('updates profile and language from the server', async () => {
        saveStoredSession(stored('a', 'en'))
        api.getCurrentSession.mockResolvedValue({
            ...created('a').session,
            profile: { ...profile('a', 'Admin'), name: 'Renamed' },
        })
        api.getSettings.mockResolvedValue({ language: 'de' })
        const session = useSessionStore()
        session.restore()

        await session.refresh()

        expect(session.profile?.name).toBe('Renamed')
        expect(session.isAdmin).toBe(true)
        expect(session.language).toBe('de')
        expect(loadStoredSession()?.profile.name).toBe('Renamed')
    })

    it('ends the session when the server rejects the token', async () => {
        saveStoredSession(stored('a'))
        api.getCurrentSession.mockRejectedValue({
            code: 'auth.unauthorized',
            responseStatusCode: 401,
        })
        const session = useSessionStore()
        session.restore()

        await session.refresh()

        expect(session.isAuthenticated).toBe(false)
        expect(loadStoredSession()).toBeNull()
    })

    it('keeps the session when the server cannot be reached', async () => {
        saveStoredSession(stored('a'))
        api.getCurrentSession.mockRejectedValue(new TypeError('Failed to fetch'))
        const session = useSessionStore()
        session.restore()

        await session.refresh()

        expect(session.isAuthenticated).toBe(true)
        expect(loadStoredSession()).not.toBeNull()
    })

    it('saves the language on the server', async () => {
        saveStoredSession(stored('a', 'en'))
        api.updateSettings.mockResolvedValue({ language: 'de' })
        const session = useSessionStore()
        session.restore()

        await session.updateLanguage('de')

        expect(api.updateSettings).toHaveBeenCalledWith({ language: 'de' })
        expect(session.language).toBe('de')
        expect(loadStoredSession()?.language).toBe('de')
    })

    it('passes errors of saving the language on', async () => {
        saveStoredSession(stored('a', 'en'))
        api.updateSettings.mockRejectedValue(new TypeError('Failed to fetch'))
        const session = useSessionStore()
        session.restore()

        await expect(session.updateLanguage('de')).rejects.toThrow()
        expect(session.language).toBe('en')
    })

    it('signs out on the server and forgets the session', async () => {
        saveStoredSession(stored('a'))
        api.deleteCurrentSession.mockResolvedValue(undefined)
        const session = useSessionStore()
        session.restore()

        await session.signOut()

        expect(api.deleteCurrentSession).toHaveBeenCalledOnce()
        expect(session.isAuthenticated).toBe(false)
        expect(sessionTokens.current).toBeNull()
        expect(loadStoredSession()).toBeNull()
    })

    it('signs out also when the server cannot be reached', async () => {
        saveStoredSession(stored('a'))
        api.deleteCurrentSession.mockRejectedValue(new TypeError('Failed to fetch'))
        const session = useSessionStore()
        session.restore()

        await session.signOut()

        expect(session.isAuthenticated).toBe(false)
    })
})
