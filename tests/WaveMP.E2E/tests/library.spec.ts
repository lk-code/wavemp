import { expect, test, type Locator, type Page } from '@playwright/test'

// Browsing and searching the library with the synthetic test media of scripts/generate-media.sh
// (scanned by library.setup.ts):
//   E2E Album (E2E Artist, 2026, cover.jpg): First Song (E2E Genre), Second Song (audio and video)
//   without album: Video Only (E2E Artist, video only), Loose Song (Loose Artist)
// With E2E_EXPECTED_TRACKS (the CI), the number of songs is checked as well.

const expectedTracks = process.env.E2E_EXPECTED_TRACKS
    ? Number(process.env.E2E_EXPECTED_TRACKS)
    : undefined

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function group(page: Page, title: string): Locator {
    return part(page, 'list-group').filter({
        has: page.locator("[data-part='list-group-title']", { hasText: title }),
    })
}

function row(page: Page, title: string): Locator {
    return part(page, 'list-row').filter({ hasText: title })
}

function tile(page: Page, title: string): Locator {
    return part(page, 'tile').filter({
        has: page.locator("[data-part='tile-title']", { hasText: title }),
    })
}

async function expectNoHorizontalScroll(page: Page): Promise<void> {
    const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )
    expect(overflow).toBeLessThanOrEqual(1)
}

async function expectLoadedCover(cover: Locator): Promise<void> {
    const image = cover.locator("[data-part='cover-image']")
    await expect(image).toBeVisible()
    await expect
        .poll(() => image.evaluate((element: HTMLImageElement) => element.naturalWidth))
        .toBeGreaterThan(0)
}

test('shows the songs grouped by album with covers and video badges', async ({ page }) => {
    await page.goto('/library/songs')

    const album = group(page, 'E2E Album')
    await expect(album.locator("[data-part='list-row']")).toHaveCount(2)
    await expect(album.locator("[data-part='list-row']").first()).toContainText('First Song')
    await expectLoadedCover(album.locator("[data-part='list-group-info'] [data-part='cover']"))
    await expect(
        row(page, 'Second Song').locator("[data-part='badge'][data-state='has-video']"),
    ).toHaveCount(1)

    const videoOnly = row(page, 'Video Only')
    await expect(videoOnly).toHaveAttribute('data-state', /\bvideo-only\b/)
    await expect(videoOnly).toHaveAttribute('data-state', /\bhas-video\b/)
    await expect(
        group(page, 'Unknown album').filter({
            has: page.locator("[data-part='list-row']", { hasText: 'Video Only' }),
        }),
    ).toHaveCount(1)
})

test('opens an album and its artist from the albums', async ({ page }) => {
    await page.goto('/library/albums')

    const album = tile(page, 'E2E Album')
    await expect(album.locator("[data-part='tile-subtitle']")).toHaveText('E2E Artist')
    await album.click()

    await expect(page).toHaveURL(/\/library\/albums\/[0-9a-f-]+$/)
    await expect(part(page, 'view')).toHaveAttribute('data-view', 'library-album')
    await expect(part(page, 'detail-title')).toHaveText('E2E Album')
    await expect(part(page, 'list-row')).toHaveCount(2)
    await expectLoadedCover(part(page, 'detail-header').locator("[data-part='cover']"))

    await part(page, 'detail-info').locator("[data-field='artist'] [data-part='link']").click()

    await expect(page).toHaveURL(/\/library\/artists\/[0-9a-f-]+$/)
    await expect(part(page, 'detail-title')).toHaveText('E2E Artist')
})

test('shows the albums and songs of an artist', async ({ page }) => {
    await page.goto('/library/artists')

    await tile(page, 'E2E Artist').click()

    await expect(part(page, 'view')).toHaveAttribute('data-view', 'library-artist')
    const albums = part(page, 'section').and(page.locator("[data-item='albums']"))
    await expect(albums.locator("[data-part='tile-title']")).toHaveText(['E2E Album'])
    const songs = part(page, 'section').and(page.locator("[data-item='tracks']"))
    await expect(songs.locator("[data-part='list-row']")).toHaveCount(3)
    await expect(songs.locator("[data-part='list-row']", { hasText: 'Video Only' })).toHaveCount(1)
})

test('shows the songs of a genre and of a year', async ({ page }) => {
    await page.goto('/library/genres')
    await tile(page, 'E2E Genre').click()

    await expect(part(page, 'view')).toHaveAttribute('data-view', 'library-genre')
    await expect(part(page, 'list-row')).toHaveCount(1)
    await expect(part(page, 'list-row')).toContainText('First Song')

    await page.goto('/library/years')
    await tile(page, '2026').click()

    await expect(part(page, 'view')).toHaveAttribute('data-view', 'library-year')
    await expect(part(page, 'detail-title')).toHaveText('2026')
    await expect(part(page, 'list-row')).toContainText(['First Song'])
})

test('shows every song under recently added', async ({ page }) => {
    await page.goto('/library/recently-added')

    await expect(row(page, 'Loose Song')).toBeVisible()
    for (const title of ['First Song', 'Second Song', 'Video Only']) {
        await expect(row(page, title)).toBeVisible()
    }
    if (expectedTracks !== undefined) {
        await expect(part(page, 'list-row')).toHaveCount(expectedTracks)
    }
})

test('searches the library despite typos', async ({ page }) => {
    await page.goto('/library/songs')
    const search = part(page, 'search').locator('input')

    await search.fill('Secnd Song')

    await expect(page).toHaveURL(/\/library\/search\?q=Secnd(\+|%20)Song$/)
    const songs = part(page, 'section').and(page.locator("[data-item='tracks']"))
    await expect(songs.locator("[data-part='list-row']").first()).toContainText('Second Song')

    await search.fill('loose artst')
    await search.press('Enter')

    const artists = part(page, 'section').and(page.locator("[data-item='artists']"))
    await expect(artists.locator("[data-part='tile-title']").first()).toHaveText('Loose Artist')

    await artists.locator(':scope > [data-part="link"]').click()

    await expect(page).toHaveURL(/\/library\/artists\?q=loose(\+|%20)artst$/)
    await expect(part(page, 'alert')).toContainText('loose artst')
    await expect(tile(page, 'Loose Artist')).toBeVisible()
})

test.describe('on the desktop', () => {
    test.skip(({ isMobile }) => isMobile, 'Desktop only')

    test('shows all files of a song to admins through its context menu', async ({ page }) => {
        await page.goto('/library/songs')

        await row(page, 'Second Song').click({ button: 'right' })
        await page.locator("[data-part='context-menu'] [data-item='details']").click()

        await expect(page).toHaveURL(/\/library\/tracks\/[0-9a-f-]+$/)
        await expect(part(page, 'detail-title')).toHaveText('Second Song')
        const files = part(page, 'file-item')
        await expect(files).toHaveCount(2)
        await expect(files.locator("[data-part='file-path']")).toHaveText([
            /02 - Second Song\.flac$/,
            /02 - Second Song\.mp4$/,
        ])
        for (const file of await files.all()) {
            await expect(file).toHaveAttribute('data-state', /\bprimary\b/)
        }
    })

    test('sorts the songs with the sort options of the toolbar', async ({ page }) => {
        await page.goto('/library/songs')
        const sort = part(page, 'sort')

        await sort.click()
        await page.locator("[data-part='context-menu'] [data-item='sort-title']").click()
        await expect(page).toHaveURL(/\/library\/songs\?sort=Title$/)
        await expect(part(page, 'list-row').first()).toContainText('First Song')

        await sort.click()
        await expect(
            page.locator("[data-part='context-menu'] [data-item='sort-title']"),
        ).toHaveAttribute('aria-checked', 'true')
        await page.locator("[data-part='context-menu'] [data-item='direction-desc']").click()

        await expect(page).toHaveURL(/\/library\/songs\?sort=Title&direction=Desc$/)
        await expect(part(page, 'list-row').first()).toContainText('Video Only')
    })
})

test.describe('on mobile', () => {
    test.skip(({ isMobile }) => !isMobile, 'Mobile only')

    test('opens an album through the drawer without horizontal scrolling', async ({ page }) => {
        await page.goto('/library/songs')

        await part(page, 'btn-menu').click()
        await page.getByRole('treeitem', { name: 'Album', exact: true }).click()
        await expect(page).toHaveURL(/\/library\/albums$/)
        await expect(tile(page, 'E2E Album')).toBeVisible()
        await expectNoHorizontalScroll(page)

        await tile(page, 'E2E Album').click()
        await expect(part(page, 'detail-title')).toHaveText('E2E Album')
        await expect(part(page, 'list-row')).toHaveCount(2)
        await expectNoHorizontalScroll(page)

        await part(page, 'search').locator('input').fill('first')
        await expect(page).toHaveURL(/\/library\/search\?q=first$/)
        await expect(part(page, 'list-row').first()).toContainText('First Song')
        await expectNoHorizontalScroll(page)
    })
})
