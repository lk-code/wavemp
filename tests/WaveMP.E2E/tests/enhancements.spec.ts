import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import { createUser, deleteUser, findTrackId, uniqueSuffix, type TestUser } from './helpers/api'
import { homeUrl, profileTile } from './helpers/navigation'
import { emptyStorageState } from './helpers/storage'

// Equalizer and sleep timer with the synthetic test media of scripts/generate-media.sh:
//   E2E Album: First Song and Second Song, two seconds each
//   Loose Song: 20 seconds
// The equalizer belongs to the profile, so every test selects a profile of its own.

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function band(page: Page, frequency: number): Locator {
    return page.locator(
        `[data-part='app'] [data-part='equalizer-band'][data-frequency='${frequency}']`,
    )
}

function presetSelect(page: Page): Locator {
    return page.locator("[data-part='app'] [data-field='equalizer-preset'] select")
}

/** The gain as the equalizer shows it: one decimal, with a sign except for zero. */
function decibels(value: number): string {
    return value === 0 ? '0.0' : `${value > 0 ? '+' : '-'}${Math.abs(value).toFixed(1)}`
}

async function sessionHeaders(
    request: APIRequestContext,
    profileId: string,
): Promise<Record<string, string>> {
    const session = await request.post('/api/v1/sessions', { data: { profileId } })
    expect(session.status()).toBe(201)
    return { Authorization: `Bearer ${(await session.json()).token}` }
}

interface StoredEqualizer {
    enabled: boolean
    preset: string
    customGainsDb: number[]
}

async function storedEqualizer(
    request: APIRequestContext,
    profileId: string,
): Promise<StoredEqualizer> {
    const response = await request.get('/api/v1/me/settings', {
        headers: await sessionHeaders(request, profileId),
    })
    expect(response.status()).toBe(200)
    return (await response.json()).equalizer
}

async function presetCurves(
    request: APIRequestContext,
    profileId: string,
): Promise<Map<string, number[]>> {
    const response = await request.get('/api/v1/equalizer', {
        headers: await sessionHeaders(request, profileId),
    })
    expect(response.status()).toBe(200)
    const presets = (await response.json()).presets as { preset: string; gainsDb: number[] }[]
    return new Map(presets.map((entry) => [entry.preset, entry.gainsDb]))
}

/** The buttons of mode-switch: in the transport bar, on phones in the full-screen player. */
async function showPlayerControls(page: Page, isMobile: boolean): Promise<void> {
    if (isMobile) {
        const transport = part(page, 'transport')
        if (!/\bexpanded\b/.test((await transport.getAttribute('data-state')) ?? '')) {
            await part(page, 'now-playing-mini').click()
            await expect(transport).toHaveAttribute('data-state', /\bexpanded\b/)
        }
    }

    await expect(part(page, 'btn-sleep-timer')).toBeVisible()
}

async function chooseSleepTimer(page: Page, item: string): Promise<void> {
    await part(page, 'btn-sleep-timer').click()
    const entry = part(page, 'sleep-timer').locator(`[data-item='${item}']`)
    await expect(entry).toBeVisible()
    await entry.click()
}

async function openAlbum(page: Page): Promise<void> {
    await page.goto('/library/albums')
    await part(page, 'tile')
        .filter({ has: page.locator("[data-part='tile-title']", { hasText: 'E2E Album' }) })
        .click()
    await expect(part(page, 'detail-title')).toHaveText('E2E Album')
}

test.describe('with a profile of its own', () => {
    test.use({ storageState: emptyStorageState, locale: 'en-US' })

    let user: TestUser

    test.beforeEach(async ({ page, request }) => {
        user = await createUser(request, `Listener ${uniqueSuffix()}`)
        await page.goto('/profiles')
        await profileTile(page, user.name).click()
        await expect(page).toHaveURL(homeUrl)
    })

    test.afterEach(async ({ request }) => {
        await deleteUser(request, user.id)
    })

    test('applies equalizer presets and keeps the own curve of the profile', async ({
        page,
        request,
        isMobile,
    }) => {
        const curves = await presetCurves(request, user.id)
        const rock = curves.get('Rock')!
        const pop = curves.get('Pop')!

        // The equalizer button of the player opens the equalizer below Now Playing.
        await showPlayerControls(page, isMobile)
        await part(page, 'btn-equalizer').click()
        await expect(page).toHaveURL(/\/now-playing\/equalizer$/)
        const equalizer = part(page, 'equalizer')
        await expect(equalizer).toHaveAttribute('data-state', 'off')
        await expect(presetSelect(page)).toHaveValue('Flat')
        await expect(band(page, 31).locator("[data-part='equalizer-value']")).toHaveText('0.0')

        await equalizer.locator("[data-item='power']").click()
        await presetSelect(page).selectOption('Rock')
        await expect(equalizer).toHaveAttribute('data-state', 'on')
        await expect(band(page, 31).locator("[data-part='equalizer-value']")).toHaveText(
            decibels(rock[0]!),
        )
        await expect
            .poll(() => storedEqualizer(request, user.id))
            .toMatchObject({ enabled: true, preset: 'Rock' })

        // The profile keeps it after loading the app again.
        await page.reload()
        await expect(presetSelect(page)).toHaveValue('Rock')
        await expect(part(page, 'equalizer')).toHaveAttribute('data-state', 'on')

        // Moving a band turns the preset into the own curve.
        const thumb = band(page, 1000).getByRole('slider')
        await thumb.focus()
        await thumb.press('ArrowUp')
        const ownValue = rock[5]! + 0.5
        await expect(presetSelect(page)).toHaveValue('Custom')
        await expect(band(page, 1000).locator("[data-part='equalizer-value']")).toHaveText(
            decibels(ownValue),
        )
        await expect
            .poll(async () => {
                const stored = await storedEqualizer(request, user.id)
                return { preset: stored.preset, gain: stored.customGainsDb[5] }
            })
            .toEqual({ preset: 'Custom', gain: ownValue })

        // Another preset and back: the own curve is still there.
        await presetSelect(page).selectOption('Pop')
        await expect(band(page, 1000).locator("[data-part='equalizer-value']")).toHaveText(
            decibels(pop[5]!),
        )
        await presetSelect(page).selectOption('Custom')
        await expect(band(page, 1000).locator("[data-part='equalizer-value']")).toHaveText(
            decibels(ownValue),
        )
        await expect(band(page, 31).locator("[data-part='equalizer-value']")).toHaveText(
            decibels(rock[0]!),
        )

        await equalizer.locator("[data-item='reset']").click()
        await expect(presetSelect(page)).toHaveValue('Flat')
        await expect.poll(async () => (await storedEqualizer(request, user.id)).preset).toBe('Flat')
    })

    test('stops at the end of the song and waits at the start of the next one', async ({
        page,
        isMobile,
    }) => {
        await openAlbum(page)
        // The songs last two seconds: pause at once and set the timer before playing on.
        await part(page, 'detail-actions').locator("[data-item='play']").click()
        await part(page, 'btn-play').click()
        await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused')
        await expect(part(page, 'now-playing-title')).toHaveText('First Song')

        await showPlayerControls(page, isMobile)
        await chooseSleepTimer(page, 'sleep-end-of-track')
        const timer = part(page, 'sleep-timer')
        await expect(timer).toHaveAttribute('data-state', /\bend-of-track\b/)
        await expect(part(page, 'btn-sleep-timer')).toHaveAttribute('aria-pressed', 'true')

        await part(page, 'btn-play').click()
        await expect(part(page, 'transport')).toHaveAttribute('data-state', /\bplaying\b/)

        // The first song ends, the player moves to the second one and waits there.
        await expect(part(page, 'now-playing-title')).toHaveText('Second Song', {
            timeout: 15_000,
        })
        await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused')
        await expect(part(page, 'seek').getByRole('slider')).toHaveAttribute('aria-valuenow', '0')
        await expect(timer).toHaveAttribute('data-state', 'off')
        await expect(part(page, 'btn-sleep-timer')).toHaveAttribute('aria-pressed', 'false')
    })

    test('pauses after the chosen time', async ({ page, request, isMobile }) => {
        await page.clock.install()
        await page.reload()

        const id = await findTrackId(request, 'Loose Song')
        await page.goto(`/library/tracks/${id}`)
        await part(page, 'detail-actions').locator("[data-item='play']").click()
        await expect(part(page, 'now-playing-title')).toHaveText('Loose Song')
        await expect(part(page, 'transport')).toHaveAttribute('data-state', /\bplaying\b/)

        await showPlayerControls(page, isMobile)
        await chooseSleepTimer(page, 'sleep-15')
        const timer = part(page, 'sleep-timer')
        await expect(timer).toHaveAttribute('data-state', 'duration')
        await expect(part(page, 'sleep-timer-remaining')).toHaveText(/^1[45]:\d\d$/)

        await page.clock.fastForward('15:05')

        await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused')
        await expect(timer).toHaveAttribute('data-state', 'off')
        await expect(part(page, 'sleep-timer-remaining')).toHaveCount(0)
        await expect(part(page, 'now-playing-title')).toHaveText('Loose Song')
    })
})
