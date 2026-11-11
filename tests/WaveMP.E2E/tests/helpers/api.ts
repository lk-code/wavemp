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

// Scans and library settings

export interface ScanSummary {
    readonly id: string
    readonly status: string
    readonly statistics?: { readonly tracksTotal: number; readonly filesFound: number } | null
}

/** True while a scan is queued or running. */
export async function isScanActive(request: APIRequestContext): Promise<boolean> {
    const response = await request.get('/api/v1/scans/current', {
        headers: authorization(await adminToken(request)),
    })
    expect([200, 404]).toContain(response.status())
    return response.status() === 200
}

/** Waits until no scan is queued or running, e.g. the periodic scan after the start. */
export async function waitForIdleScan(request: APIRequestContext): Promise<void> {
    await expect
        .poll(() => isScanActive(request), { timeout: 120_000, intervals: [500] })
        .toBe(false)
}

/** The newest scan, or undefined if there was none. */
export async function latestScan(request: APIRequestContext): Promise<ScanSummary | undefined> {
    const response = await request.get('/api/v1/scans?limit=1', {
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(200)
    return ((await response.json()).items as ScanSummary[])[0]
}

/** Id of the newest run of a job, or undefined. */
export async function latestJobRunId(
    request: APIRequestContext,
    jobId: string,
): Promise<string | undefined> {
    const response = await request.get(`/api/v1/jobs/${jobId}/runs?limit=1`, {
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(200)
    return ((await response.json()).items as { id: string }[])[0]?.id
}

export async function updateLibrarySettings(
    request: APIRequestContext,
    changes: Record<string, unknown>,
): Promise<void> {
    const response = await request.patch('/api/v1/settings/library', {
        data: changes,
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(200)
}

/** Restores the defaults of the settings the tests change. */
export async function resetLibrarySettings(request: APIRequestContext): Promise<void> {
    await updateLibrarySettings(request, { scanIntervalMinutes: 360 })
}

// Library

/**
 * Scans the media folder and waits until the scan has finished. A scan that is already running
 * (e.g. the periodic one) counts as well.
 */
export async function scanLibrary(request: APIRequestContext): Promise<void> {
    await waitForIdleScan(request)
    const response = await request.post('/api/v1/scans', {
        headers: authorization(await adminToken(request)),
    })
    expect([202, 409]).toContain(response.status())
    await waitForIdleScan(request)
}

/** Titles of all tracks of the library, in the default order. */
export async function listTrackTitles(request: APIRequestContext): Promise<string[]> {
    const response = await request.get('/api/v1/tracks?limit=100', {
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(200)
    return ((await response.json()).items as { title: string }[]).map((track) => track.title)
}

/** Id of the track with exactly this title. */
export async function findTrackId(request: APIRequestContext, title: string): Promise<string> {
    const response = await request.get(`/api/v1/tracks?q=${encodeURIComponent(title)}&limit=100`, {
        headers: authorization(await adminToken(request)),
    })
    expect(response.status()).toBe(200)
    const track = ((await response.json()).items as { id: string; title: string }[]).find(
        (item) => item.title === title,
    )
    expect(track, `The library has no song "${title}".`).toBeDefined()
    return track!.id
}

/** Writes metadata into the audio file of a track; null removes a value. */
export async function updateTrackMetadata(
    request: APIRequestContext,
    id: string,
    changes: Record<string, string | number | null>,
): Promise<void> {
    const response = await request.patch(`/api/v1/tracks/${id}/metadata`, {
        data: changes,
        headers: authorization(await adminToken(request)),
    })
    expect(response.status(), await response.text()).toBe(200)
}
