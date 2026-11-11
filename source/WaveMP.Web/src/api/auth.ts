// Authentication of the API client with the token of the current session. The session store
// sets the token; every request to the own server sends it as bearer token. A 401 answer to a
// request with the current token means that the session has ended (switched on another device,
// profile deleted, expired), so the session store is told to forget it.

import {
    AllowedHostsValidator,
    type AccessTokenProvider,
    type RequestOption,
} from '@microsoft/kiota-abstractions'
import type { Middleware } from '@microsoft/kiota-http-fetchlibrary'

export interface SessionTokens {
    /** Bearer token of the current session; null without a selected profile. */
    current: string | null
    /** Called when the server rejects the current token. */
    onUnauthorized: (() => void) | null
}

/** Token of the current session, shared by the API client and the session store. */
export const sessionTokens: SessionTokens = { current: null, onUnauthorized: null }

const bearerPrefix = /^Bearer\s+/i

/** Provides the session token, but only for requests to the origin of the API. */
export class SessionAccessTokenProvider implements AccessTokenProvider {
    private readonly origin: string
    private readonly validator: AllowedHostsValidator

    constructor(
        baseUrl: string,
        private readonly tokens: SessionTokens = sessionTokens,
    ) {
        const url = new URL(baseUrl)
        this.origin = url.origin
        this.validator = new AllowedHostsValidator(new Set([url.hostname]))
    }

    async getAuthorizationToken(url?: string): Promise<string> {
        if (!url || !this.isOwnOrigin(url)) {
            return ''
        }

        return this.tokens.current ?? ''
    }

    getAllowedHostsValidator(): AllowedHostsValidator {
        return this.validator
    }

    private isOwnOrigin(url: string): boolean {
        try {
            return new URL(url, this.origin).origin === this.origin
        } catch {
            return false
        }
    }
}

/** Kiota middleware that reports a rejected session token to the session store. */
export class UnauthorizedHandler implements Middleware {
    next: Middleware | undefined

    constructor(private readonly tokens: SessionTokens = sessionTokens) {}

    async execute(
        url: string,
        requestInit: RequestInit,
        requestOptions?: Record<string, RequestOption>,
    ): Promise<Response> {
        if (!this.next) {
            throw new Error('The UnauthorizedHandler needs a next middleware.')
        }

        const sent = new Headers(requestInit.headers)
            .get('Authorization')
            ?.replace(bearerPrefix, '')
        const response = await this.next.execute(url, requestInit, requestOptions)

        // Only the current token ends the session: a late answer to a request with an old token
        // (e.g. sent before switching the profile) must not end the new session.
        if (response.status === 401 && sent && sent === this.tokens.current) {
            this.tokens.onUnauthorized?.()
        }

        return response
    }
}
