import { expect, test } from '@playwright/test'

// The web app answers every client route, so these make sure API routes are never answered with it.

test('unknown API routes return Problem Details instead of the web app', async ({ request }) => {
    const response = await request.get('/api/v1/does-not-exist')

    expect(response.status()).toBe(404)
    expect(response.headers()['content-type']).toContain('application/problem+json')
    expect((await response.json()).code).toBe('request.not_found')
})

test('unsupported methods on API routes return 405 with an Allow header', async ({ request }) => {
    const response = await request.post('/api/v1/health')

    expect(response.status()).toBe(405)
    expect(response.headers()['allow']).toBe('GET')
    expect(response.headers()['content-type']).toContain('application/problem+json')
    expect((await response.json()).code).toBe('request.failed')
})

test('health reports the version of the image', async ({ request }) => {
    const response = await request.get('/api/v1/health')

    expect(response.status()).toBe(200)
    const { version } = await response.json()
    // Release builds pass the tag version; other builds keep the version from Directory.Build.props.
    const expected = process.env.E2E_EXPECTED_VERSION
    if (expected) {
        expect(version).toBe(expected)
    } else {
        expect(version).toMatch(/^\d+\.\d+\.\d+/)
    }
})
