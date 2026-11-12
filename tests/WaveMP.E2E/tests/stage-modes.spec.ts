import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import {
    adminHeaders,
    createUser,
    deleteUser,
    findTrackId,
    uniqueSuffix,
    type TestUser,
} from './helpers/api'
import { homeUrl, profileTile } from './helpers/navigation'
import { adminStorageState, emptyStorageState } from './helpers/storage'

// The views of Now Playing (cover, video, visualization) with the synthetic test media of
// scripts/generate-media.sh:
//   Loose Song: 20 seconds of audio with a WebM video the browser plays as it is
//   First Song: audio without a video
//   Second Song: audio with a video; the video sync test changes its offset and resets it
// The chosen view belongs to the profile, so the tests that switch it select their own profile.

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function stageView(page: Page, kind: string): Locator {
    return page.locator(`[data-part='app'] [data-part='stage-view'][data-kind='${kind}']`)
}

/** Opens Now Playing without loading the page again, so the player keeps its track. */
async function openNowPlaying(page: Page): Promise<void> {
    await page.locator("[data-part='app'] a[href='/now-playing']").filter({ visible: true }).click()
    await expect(page).toHaveURL(/\/now-playing$/)
}

async function playSong(page: Page, id: string, title: string): Promise<void> {
    await page.goto(`/library/tracks/${id}`)
    await part(page, 'detail-actions').locator("[data-item='play']").click()
    await expect(part(page, 'now-playing-title')).toHaveText(title)
}

/** Position of the audio as the seek bar shows it, in seconds. */
function audioPosition(page: Page): Promise<number> {
    return page
        .locator("[data-part='app'] [data-part='seek'] [role='slider']")
        .evaluate((element) => Number(element.getAttribute('aria-valuenow')))
}

/** Distance in seconds between the video and the audio, read at the same moment. */
function videoDrift(page: Page): Promise<number> {
    return page.evaluate(() => {
        const slider = document.querySelector("[data-part='seek'] [role='slider']")
        const video = document.querySelector<HTMLVideoElement>("[data-part='video']")
        if (!slider || !video) {
            return Number.POSITIVE_INFINITY
        }
        return Math.abs(video.currentTime - Number(slider.getAttribute('aria-valuenow')))
    })
}

/** Waits until the audio has played on for one more second. */
async function playsOn(page: Page): Promise<void> {
    const from = await audioPosition(page)
    await expect.poll(() => audioPosition(page), { timeout: 10_000 }).toBeGreaterThan(from + 1)
}

/**
 * Waits until the video shows the position of the audio. The seek bar follows the audio a little
 * behind, so the distance may stay a few tenths of a second.
 */
async function expectVideoInSync(page: Page): Promise<void> {
    await expect.poll(() => videoDrift(page), { timeout: 10_000 }).toBeLessThan(0.75)
}

/**
 * Records the position of the audio in the page every 100 milliseconds, so a test can tell
 * afterwards whether the song ever started again or stopped.
 */
async function recordPositions(page: Page): Promise<void> {
    await page.evaluate(() => {
        const positions: number[] = []
        ;(window as unknown as { e2ePositions: number[] }).e2ePositions = positions
        window.setInterval(() => {
            const slider = document.querySelector("[data-part='seek'] [role='slider']")
            positions.push(Number(slider?.getAttribute('aria-valuenow') ?? Number.NaN))
        }, 100)
    })
}

function recordedPositions(page: Page): Promise<number[]> {
    return page.evaluate(() => (window as unknown as { e2ePositions: number[] }).e2ePositions)
}

async function sessionHeaders(
    request: APIRequestContext,
    profileId: string,
): Promise<Record<string, string>> {
    const session = await request.post('/api/v1/sessions', { data: { profileId } })
    expect(session.status()).toBe(201)
    return { Authorization: `Bearer ${(await session.json()).token}` }
}

async function storedViewMode(request: APIRequestContext, profileId: string): Promise<string> {
    const response = await request.get('/api/v1/me/settings', {
        headers: await sessionHeaders(request, profileId),
    })
    expect(response.status()).toBe(200)
    return (await response.json()).playerViewMode
}

test.describe('with a profile of its own', () => {
    test.use({ storageState: emptyStorageState, locale: 'en-US' })

    let user: TestUser

    test.beforeEach(async ({ page, request }) => {
        user = await createUser(request, `Viewer ${uniqueSuffix()}`)
        await page.goto('/profiles')
        await profileTile(page, user.name).click()
        await expect(page).toHaveURL(homeUrl)
    })

    test.afterEach(async ({ request }) => {
        await deleteUser(request, user.id)
    })

    test('switches the view while the song plays on without a break', async ({ page, request }) => {
        const id = await findTrackId(request, 'Loose Song')
        await playSong(page, id, 'Loose Song')
        await openNowPlaying(page)
        await recordPositions(page)

        // A new profile starts with the video, which follows the audio.
        const stage = part(page, 'stage')
        await expect(stage).toHaveAttribute('data-state', 'video', { timeout: 15_000 })
        await expect(part(page, 'transport')).toHaveAttribute('data-state', /\bplaying\b/)
        await playsOn(page)
        await expectVideoInSync(page)

        await part(page, 'btn-view-cover').click()
        await expect(stage).toHaveAttribute('data-state', 'cover')
        await expect(part(page, 'btn-view-cover')).toHaveAttribute('aria-pressed', 'true')
        await expect(stageView(page, 'cover')).toHaveAttribute('data-state', 'active')
        await expect(stageView(page, 'video')).not.toHaveAttribute('data-state', 'active')
        await expect.poll(() => storedViewMode(request, user.id)).toBe('Cover')
        await playsOn(page)

        await part(page, 'btn-view-visualization').click()
        await expect(stage).toHaveAttribute('data-state', 'visualization')
        await expect(part(page, 'visualization')).toBeVisible()
        await expect.poll(() => storedViewMode(request, user.id)).toBe('Visualization')
        await playsOn(page)

        // Back to the video: it catches up with the audio, which went on in the meantime.
        await part(page, 'btn-view-video').click()
        await expect(stage).toHaveAttribute('data-state', 'video')
        await expect(part(page, 'video')).toBeVisible()
        await expect.poll(() => storedViewMode(request, user.id)).toBe('Video')
        await expectVideoInSync(page)

        // The audio led all the time: it played on and never started again.
        await expect(part(page, 'transport')).toHaveAttribute('data-state', /\bplaying\b/)
        const positions = await recordedPositions(page)
        expect(positions.length).toBeGreaterThan(10)
        for (let index = 1; index < positions.length; index++) {
            expect(positions[index]).toBeGreaterThanOrEqual(positions[index - 1]!)
        }
        expect(positions.at(-1)!).toBeGreaterThan(3)
    })

    test('remembers the view of the profile and offers the video only for songs with one', async ({
        page,
        request,
    }) => {
        const firstSong = await findTrackId(request, 'First Song')
        const looseSong = await findTrackId(request, 'Loose Song')

        // First Song has no video: the stage shows its cover, and the video cannot be chosen.
        await playSong(page, firstSong, 'First Song')
        await openNowPlaying(page)
        await expect(part(page, 'stage')).toHaveAttribute('data-state', 'cover')
        await expect(part(page, 'btn-view-video')).toBeDisabled()
        await expect(part(page, 'btn-view-cover')).toHaveAttribute('aria-pressed', 'true')
        await expect(stageView(page, 'video')).toHaveCount(0)

        await part(page, 'btn-view-visualization').click()
        await expect(part(page, 'stage')).toHaveAttribute('data-state', 'visualization')
        await expect.poll(() => storedViewMode(request, user.id)).toBe('Visualization')

        // After loading the app again, the profile keeps its view, also for a song with a video.
        await page.reload()
        await playSong(page, looseSong, 'Loose Song')
        await openNowPlaying(page)
        await expect(part(page, 'stage')).toHaveAttribute('data-state', 'visualization')
        await expect(part(page, 'btn-view-visualization')).toHaveAttribute('aria-pressed', 'true')
        await expect(part(page, 'btn-view-video')).toBeEnabled()
    })
})

test.describe('video sync', () => {
    test.use({ storageState: adminStorageState, locale: 'en-US' })

    test('sets the video offset of a song through the API and the song page', async ({
        page,
        request,
    }, testInfo) => {
        test.skip(
            testInfo.project.name !== 'chromium',
            'Changes the song; runs in one project only',
        )
        const id = await findTrackId(request, 'Second Song')
        const headers = await adminHeaders(request)
        const offsetUrl = `/api/v1/tracks/${id}/video/offset`

        try {
            const outOfRange = await request.put(offsetUrl, {
                data: { offsetMilliseconds: 700_000 },
                headers,
            })
            expect(outOfRange.status()).toBe(400)
            expect(await outOfRange.text()).toContain('out_of_range')

            const stored = await request.put(offsetUrl, {
                data: { offsetMilliseconds: 1500 },
                headers,
            })
            expect(stored.status()).toBe(200)
            expect(await stored.json()).toMatchObject({ trackId: id, offsetMilliseconds: 1500 })

            // The players get the offset with the video.
            const playback = await request.get(
                `/api/v1/tracks/${id}/video/playback?videoContainers=Mp4&videoCodecs=H264&audioCodecs=Aac`,
                { headers },
            )
            expect(playback.status()).toBe(200)
            expect((await playback.json()).offsetMilliseconds).toBe(1500)

            await page.goto(`/library/tracks/${id}`)
            const section = page.locator("[data-part='app'] [data-item='video-sync']")
            await expect(section.locator("[data-part='section-title']")).toHaveText('Video sync')
            const field = section.locator("[data-field='video-offset'] input")
            await expect(field).toHaveValue('1500')
            await field.fill('500')
            await section.locator("[data-item='save-video-offset']").click()
            await expect(section.locator("[data-part='alert']")).toHaveText('Saved.')

            const response = await request.get(offsetUrl, { headers })
            expect(response.status()).toBe(200)
            expect((await response.json()).offsetMilliseconds).toBe(500)
        } finally {
            const reset = await request.put(offsetUrl, { data: { offsetMilliseconds: 0 }, headers })
            expect(reset.status()).toBe(200)
        }
    })

    test('keeps the video offset to admins', async ({ request }) => {
        const id = await findTrackId(request, 'Second Song')
        const user = await createUser(request, `Listener ${uniqueSuffix()}`)
        try {
            const headers = await sessionHeaders(request, user.id)
            const read = await request.get(`/api/v1/tracks/${id}/video/offset`, { headers })
            expect(read.status()).toBe(403)
            const write = await request.put(`/api/v1/tracks/${id}/video/offset`, {
                data: { offsetMilliseconds: 100 },
                headers,
            })
            expect(write.status()).toBe(403)
        } finally {
            await deleteUser(request, user.id)
        }
    })
})
