import { expect, test } from '@playwright/test'
import { adminHeaders, adminToken, findAlbumId, latestJobRunId } from './helpers/api'

// Scaled covers of the API with the synthetic test media (E2E Album has cover.jpg, 300 x 300).

test('delivers a scaled cover as WebP that the browser may keep', async ({ request }) => {
    const album = await findAlbumId(request, 'E2E Album')

    const response = await request.get(`/api/v1/albums/${album}/cover?size=96`, {
        headers: await adminHeaders(request),
    })

    expect(response.status()).toBe(200)
    expect(response.headers()['content-type']).toBe('image/webp')
    expect(response.headers()['etag']).toMatch(/^"[0-9a-f]{32}-f-[0-9a-f]{16}-96"$/)
    expect(response.headers()['cache-control']).toBe('private, no-cache')
    expect(response.headers()['last-modified']).toBeTruthy()
    const body = await response.body()
    expect(body.subarray(0, 4).toString('ascii')).toBe('RIFF')
    expect(body.subarray(8, 12).toString('ascii')).toBe('WEBP')
})

test('answers 304 while the cover did not change', async ({ request }) => {
    const album = await findAlbumId(request, 'E2E Album')
    const path = `/api/v1/albums/${album}/cover?size=300`
    const first = await request.get(path, { headers: await adminHeaders(request) })
    const etag = first.headers()['etag']
    expect(etag).toBeTruthy()

    const second = await request.get(path, {
        headers: await adminHeaders(request, { 'If-None-Match': etag! }),
    })

    expect(second.status()).toBe(304)
    expect((await second.body()).length).toBe(0)
})

test('rejects sizes outside the range', async ({ request }) => {
    const album = await findAlbumId(request, 'E2E Album')

    const response = await request.get(`/api/v1/albums/${album}/cover?size=0`, {
        headers: await adminHeaders(request),
    })

    expect(response.status()).toBe(400)
    expect((await response.json()).code).toBe('request.invalid')
})

test('prepares the covers after the scan', async ({ request }) => {
    await expect
        .poll(
            async () => {
                const runId = await latestJobRunId(request, 'cover-cache')
                if (!runId) {
                    return 'none'
                }

                const response = await request.get(`/api/v1/jobs/cover-cache/runs/${runId}`, {
                    headers: { Authorization: `Bearer ${await adminToken(request)}` },
                })
                return (await response.json()).status as string
            },
            { timeout: 60_000, intervals: [500] },
        )
        .toBe('Succeeded')
})
