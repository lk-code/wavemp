import { expect, test, type Locator, type Page } from '@playwright/test'
import { adminHeaders, findTrackId } from './helpers/api'
import { adminNavItem } from './helpers/navigation'

// Videos and transcoding with the synthetic test media of scripts/generate-media.sh:
//   Loose Song: MP3 with a VP9/Opus WebM video, which the browser plays as it is
//   Second Song: FLAC with an MPEG-4 Part 2 video, which no browser plays: the server transcodes
//   it to HLS with ffmpeg (in software, the test machine has no render device)

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

/** Opens Now Playing without loading the page again, so the player keeps its track. */
async function openNowPlaying(page: Page): Promise<void> {
    await page.locator("[data-part='app'] a[href='/now-playing']").filter({ visible: true }).click()
    await expect(page).toHaveURL(/\/now-playing$/)
}

async function playSong(page: Page, id: string): Promise<void> {
    await page.goto(`/library/tracks/${id}`)
    await part(page, 'detail-actions').locator("[data-item='play']").click()
    await expect(part(page, 'now-playing-title')).not.toBeEmpty()
}

/** Ready state of the video element: 1 and more once its metadata is loaded. */
function readyState(video: Locator): Promise<number> {
    return video.evaluate((element: HTMLVideoElement) => element.readyState)
}

test('shows the video of a song on the Now Playing stage and switches back to the cover', async ({
    page,
    request,
}) => {
    const id = await findTrackId(request, 'Loose Song')
    await playSong(page, id)
    await openNowPlaying(page)

    const stage = part(page, 'stage')
    await expect(stage).toHaveAttribute('data-state', 'video')
    await expect(part(page, 'btn-video')).toHaveAttribute('aria-pressed', 'true')
    const video = part(page, 'video')
    await expect(video).toBeVisible()
    // The browser plays the WebM file as it is, through the signed URL of the video.
    await expect
        .poll(() => video.evaluate((element: HTMLVideoElement) => element.currentSrc))
        .toMatch(new RegExp(`/api/v1/tracks/${id}/video\\?.*sig=`))
    await expect.poll(() => readyState(video), { timeout: 15_000 }).toBeGreaterThanOrEqual(1)
    await expect(part(page, 'btn-fullscreen')).toBeEnabled()

    await part(page, 'btn-video').click()

    await expect(stage).toHaveAttribute('data-state', 'cover')
    await expect(part(page, 'video-stage')).toHaveCount(0)
    await expect(part(page, 'stage-title')).toHaveText('Loose Song')
})

test('plays a video the browser cannot play as HLS', async ({ page, request }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium', 'Transcodes on one project only.')
    const id = await findTrackId(request, 'Second Song')
    await playSong(page, id)
    await openNowPlaying(page)

    const video = part(page, 'video')
    await expect(video).toBeVisible()
    // hls.js plays the transcoded stream through Media Source Extensions.
    await expect
        .poll(() => video.evaluate((element: HTMLVideoElement) => element.currentSrc))
        .toMatch(/^blob:/)
    await expect.poll(() => readyState(video), { timeout: 60_000 }).toBeGreaterThanOrEqual(1)
})

test('transcodes the video to HLS behind signed URLs', async ({ request }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium', 'Transcodes on one project only.')
    const id = await findTrackId(request, 'Second Song')
    const headers = await adminHeaders(request)

    const playback = await request.get(
        `/api/v1/tracks/${id}/video/playback?videoContainers=Mp4&videoCodecs=H264&audioCodecs=Aac`,
        { headers },
    )
    expect(playback.status()).toBe(200)
    const decision = await playback.json()
    expect(decision).toMatchObject({ method: 'Transcode', protocol: 'Hls' })
    const masterUrl = new URL(decision.streamUrl, 'http://wavemp')
    expect(masterUrl.pathname).toBe(`/api/v1/tracks/${id}/hls/master.m3u8`)

    // Without the signature the stream is closed.
    const unsigned = await request.get(masterUrl.pathname)
    expect(unsigned.status()).toBe(401)

    // Players get no token header: the URLs of the playlists carry the signature.
    const master = await request.get(masterUrl.pathname + masterUrl.search)
    expect(master.status()).toBe(200)
    expect(master.headers()['content-type']).toContain('application/vnd.apple.mpegurl')
    const masterText = await master.text()
    expect(masterText).toContain('#EXT-X-STREAM-INF')
    const mediaLine = masterText.split('\n').find((line) => line !== '' && !line.startsWith('#'))
    const mediaUrl = new URL(mediaLine!, masterUrl)

    const media = await request.get(mediaUrl.pathname + mediaUrl.search)
    expect(media.status()).toBe(200)
    const mediaText = await media.text()
    expect(mediaText).toContain('#EXT-X-ENDLIST')
    const segmentLine = mediaText.split('\n').find((line) => line !== '' && !line.startsWith('#'))
    const segmentUrl = new URL(segmentLine!, mediaUrl)

    // The first segment is ready once ffmpeg wrote it; until then the server answers 503.
    let segmentType = ''
    await expect
        .poll(
            async () => {
                const segment = await request.get(segmentUrl.pathname + segmentUrl.search)
                segmentType = segment.headers()['content-type'] ?? ''
                return segment.status()
            },
            { timeout: 60_000, intervals: [500, 1000, 2000] },
        )
        .toBe(200)
    expect(segmentType).toContain('video/mp2t')
})

test('shows the transcoding page of the administration', async ({ page }) => {
    await page.goto('/admin/transcoding')

    await expect(page.locator("[data-view='admin-transcoding']")).toBeVisible()
    await expect(adminNavItem(page, 'admin-transcoding')).toHaveAttribute('aria-current', 'page')
    const status = page.locator("[data-part='transcoding-status']")
    await expect(status.locator("[data-item='ffmpeg'] [data-part='stat-value']")).toHaveText('Yes')
    await expect(status.locator("[data-item='hardware'] [data-part='badge']")).toHaveAttribute(
        'data-state',
        /^(active|unavailable|disabled|failed)$/,
    )
    await expect(page.getByLabel('Hardware acceleration')).toBeVisible()
    await expect(page.getByLabel('Parallel transcodes')).toBeVisible()

    const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )
    expect(overflow).toBeLessThanOrEqual(0)
})
