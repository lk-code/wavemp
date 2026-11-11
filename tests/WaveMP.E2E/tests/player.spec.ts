import { expect, test, type Locator, type Page } from '@playwright/test'
import { adminHeaders } from './helpers/api'

// Plays the synthetic test media of scripts/generate-media.sh (scanned by library.setup.ts) in the
// browser: E2E Album holds First Song (MP3) and Second Song (FLAC), both two seconds long.

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function transport(page: Page): Locator {
    return part(page, 'transport')
}

async function openAlbum(page: Page): Promise<void> {
    await page.goto('/library/albums')
    await part(page, 'tile')
        .filter({
            has: page.locator("[data-part='tile-title']", {
                hasText: 'E2E Album',
            }),
        })
        .click()
    await expect(part(page, 'detail-title')).toHaveText('E2E Album')
}

interface HistoryEntry {
    playedAt: string
    track: { title: string }
}

test('plays an album, skips to the next song and records the play', async ({
    page,
    request,
    isMobile,
}) => {
    const startedAt = Date.now() - 1000
    await openAlbum(page)

    // The songs last two seconds: pausing right away keeps the first song from ending before the
    // test skips it.
    await part(page, 'detail-actions').locator("[data-item='play']").click()
    await part(page, 'btn-play').click()

    await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused')
    await expect(transport(page)).not.toHaveAttribute('data-state', /\bloading\b/)
    await expect(part(page, 'now-playing-title')).toHaveText('First Song')
    await expect(part(page, 'now-playing-artist')).toHaveText('E2E Artist')
    if (isMobile) {
        await part(page, 'now-playing-mini').click()
        await expect(transport(page)).toHaveAttribute('data-state', /\bexpanded\b/)
    }
    await part(page, 'btn-next').click()
    await expect(part(page, 'now-playing-title')).toHaveText('Second Song')
    await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused')

    await part(page, 'btn-play').click()
    await expect(transport(page)).toHaveAttribute('data-state', /\bplaying\b/)

    // The last song plays to its end, then the player pauses on it.
    await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused', {
        timeout: 15_000,
    })
    await expect(part(page, 'now-playing-title')).toHaveText('Second Song')

    // More than half of the song played, so it is in the history of the profile.
    const headers = await adminHeaders(request)
    await expect
        .poll(
            async () => {
                const response = await request.get('/api/v1/me/history?limit=50', { headers })
                expect(response.status()).toBe(200)
                const entries = (await response.json()).items as HistoryEntry[]
                return entries.some(
                    (entry) =>
                        entry.track.title === 'Second Song' &&
                        Date.parse(entry.playedAt) >= startedAt,
                )
            },
            { timeout: 10_000 },
        )
        .toBe(true)
})

test('plays a song of the list by double click and shows the queue', async ({ page, isMobile }) => {
    test.skip(isMobile, 'Phones start a song with a tap, the desktop with a double click.')
    await openAlbum(page)

    const second = part(page, 'list-row').filter({ hasText: 'Second Song' })
    await second.dblclick()

    await expect(part(page, 'now-playing-title')).toHaveText('Second Song')
    await expect(second).toHaveAttribute('data-state', /\bplaying\b/)

    await part(page, 'now-playing-mini').click()
    await expect(page).toHaveURL(/\/now-playing$/)
    await expect(part(page, 'stage-title')).toHaveText('Second Song')
    const items = part(page, 'queue-item')
    await expect(items.locator("[data-part='queue-item-title']")).toHaveText([
        'First Song',
        'Second Song',
    ])
    await expect(items.nth(0)).toHaveAttribute('data-state', 'played')
    await expect(items.nth(1)).toHaveAttribute('data-state', 'current')

    await items.nth(0).locator("[data-part='queue-item-info']").click()
    await expect(part(page, 'stage-title')).toHaveText('First Song')
    await expect(items.nth(0)).toHaveAttribute('aria-current', 'true')
})
