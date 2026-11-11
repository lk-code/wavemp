import { describe, expect, it, vi } from 'vitest'
import type { Middleware } from '@microsoft/kiota-http-fetchlibrary'
import { SessionAccessTokenProvider, UnauthorizedHandler, type SessionTokens } from '../auth'

function tokens(current: string | null = null): SessionTokens {
    return { current, onUnauthorized: vi.fn() }
}

function respondingWith(status: number, onRequest?: () => void): Middleware {
    return {
        next: undefined,
        execute: vi.fn(async () => {
            onRequest?.()
            return new Response(null, { status })
        }),
    }
}

function handler(state: SessionTokens, next: Middleware): UnauthorizedHandler {
    const result = new UnauthorizedHandler(state)
    result.next = next
    return result
}

function request(token?: string): RequestInit {
    return { headers: token ? { Authorization: `Bearer ${token}` } : {} }
}

describe('SessionAccessTokenProvider', () => {
    it('provides the current token for the own origin', async () => {
        const state = tokens('abc')
        const provider = new SessionAccessTokenProvider('http://localhost:8081', state)

        expect(await provider.getAuthorizationToken('http://localhost:8081/api/v1/users')).toBe(
            'abc',
        )

        state.current = 'def'
        expect(await provider.getAuthorizationToken('http://localhost:8081/api/v1/users')).toBe(
            'def',
        )
    })

    it('provides no token without a session', async () => {
        const provider = new SessionAccessTokenProvider('http://localhost:8081', tokens())

        expect(await provider.getAuthorizationToken('http://localhost:8081/api/v1/users')).toBe('')
    })

    it('never sends the token to another origin', async () => {
        const provider = new SessionAccessTokenProvider('http://localhost:8081', tokens('abc'))

        expect(await provider.getAuthorizationToken('http://example.com/api/v1/users')).toBe('')
        expect(await provider.getAuthorizationToken('http://localhost:9999/api/v1/users')).toBe('')
        expect(await provider.getAuthorizationToken('https://localhost:8081/api/v1/users')).toBe('')
        expect(await provider.getAuthorizationToken(undefined)).toBe('')
    })

    it('allows only the host of the API', () => {
        const provider = new SessionAccessTokenProvider('http://wavemp.local:8081', tokens())

        expect(provider.getAllowedHostsValidator().getAllowedHosts()).toEqual(['wavemp.local'])
    })
})

describe('UnauthorizedHandler', () => {
    it('reports a 401 answer to the current token', async () => {
        const state = tokens('abc')

        await handler(state, respondingWith(401)).execute('/api/v1/users', request('abc'))

        expect(state.onUnauthorized).toHaveBeenCalledOnce()
    })

    it('ignores a 401 answer to an older token', async () => {
        const state = tokens('old')
        const next = respondingWith(401, () => {
            state.current = 'new'
        })

        await handler(state, next).execute('/api/v1/users', request('old'))

        expect(state.onUnauthorized).not.toHaveBeenCalled()
    })

    it('ignores a 401 answer to a request without token', async () => {
        const state = tokens('abc')

        await handler(state, respondingWith(401)).execute('/api/v1/users', request())

        expect(state.onUnauthorized).not.toHaveBeenCalled()
    })

    it('ignores other status codes', async () => {
        const state = tokens('abc')

        for (const status of [200, 403, 404, 500]) {
            const response = await handler(state, respondingWith(status)).execute(
                '/api/v1/users',
                request('abc'),
            )
            expect(response.status).toBe(status)
        }

        expect(state.onUnauthorized).not.toHaveBeenCalled()
    })

    it('needs a next middleware', async () => {
        await expect(new UnauthorizedHandler(tokens()).execute('/', request())).rejects.toThrow()
    })
})
