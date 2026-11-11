import { expect, type APIRequestContext } from '@playwright/test'

// Test data through the API, independent of the browser state of the tests.

export interface TestUser {
    readonly id: string
    readonly name: string
}

/** A short random text for unique profile names. */
export function uniqueSuffix(): string {
    return Math.random().toString(36).slice(2, 8)
}

/** Token of a new session of the built-in admin profile. */
export async function adminToken(request: APIRequestContext): Promise<string> {
    const profiles = await request.get('/api/v1/profiles?limit=100')
    expect(profiles.status()).toBe(200)
    const admin = ((await profiles.json()).items as { id: string; isBuiltIn: boolean }[]).find(
        (profile) => profile.isBuiltIn,
    )
    expect(admin).toBeDefined()

    const session = await request.post('/api/v1/sessions', { data: { profileId: admin!.id } })
    expect(session.status()).toBe(201)
    return (await session.json()).token
}

function authorization(token: string) {
    return { Authorization: `Bearer ${token}` }
}

export async function createUser(request: APIRequestContext, name: string): Promise<TestUser> {
    const response = await request.post('/api/v1/users', {
        data: { name },
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(201)
    const user = await response.json()
    return { id: user.id, name: user.name }
}

export async function findUserByName(
    request: APIRequestContext,
    name: string,
): Promise<TestUser | undefined> {
    const response = await request.get('/api/v1/users?limit=100', {
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(200)
    return ((await response.json()).items as TestUser[]).find((user) => user.name === name)
}

/** Deletes a user; a user that is already gone is fine. */
export async function deleteUser(request: APIRequestContext, id: string): Promise<void> {
    const response = await request.delete(`/api/v1/users/${id}`, {
        headers: authorization(await adminToken(request)),
    })
    expect([204, 404]).toContain(response.status())
}
