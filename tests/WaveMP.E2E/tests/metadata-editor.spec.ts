import { expect, test, type Locator, type Page } from '@playwright/test'
import { findTrackId, updateLibrarySettings, updateTrackMetadata } from './helpers/api'

// The metadata editor with the synthetic test media of scripts/generate-media.sh:
//   Video Only: a song with a video only, so the editor is greyed out
//   Loose Song: an MP3 without tags in the root; the test writes genre and year and removes them
//   again afterwards, so the other tests keep finding E2E Genre and 2026 for First Song only.
// The media folder must be writable for the container (see README).

test.describe.configure({ mode: 'serial' })

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function openDialog(page: Page): Locator {
    return page.locator("[data-part='dialog'][open]")
}

function field(dialog: Locator, name: string): Locator {
    return dialog.locator(`[data-part='field'][data-field='${name}'] input`)
}

async function expectNoHorizontalScroll(page: Page): Promise<void> {
    const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )
    expect(overflow).toBeLessThanOrEqual(1)
}

test('shows the editor greyed out with a hint for a song with a video only', async ({
    page,
    request,
    isMobile,
}) => {
    const id = await findTrackId(request, 'Video Only')
    await page.goto(`/library/tracks/${id}`)

    await part(page, 'detail-actions').locator("[data-item='edit-metadata']").click()

    const dialog = openDialog(page)
    await expect(dialog).toBeVisible()
    await expect(field(dialog, 'title')).toHaveValue('Video Only')
    for (const name of ['title', 'artist', 'year', 'genre']) {
        await expect(field(dialog, name)).toBeDisabled()
    }
    await expect(dialog.locator("[data-part='dialog-footer'] button[type='submit']")).toBeDisabled()
    await expect(dialog.locator("[data-part='alert'][data-state='info']")).toBeVisible()
    if (isMobile) {
        await expectNoHorizontalScroll(page)
    }
})

test('edits the genre and year of a song', async ({ page, request }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium', 'Writes the file; runs in one project only')

    const id = await findTrackId(request, 'Loose Song')
    // The editor reads the song again itself. Without the file watcher, its writes start no scan
    // that could meet the scan tests running at the same time.
    await updateLibrarySettings(request, { fileWatcherEnabled: false })
    try {
        await page.goto(`/library/tracks/${id}`)
        await part(page, 'detail-actions').locator("[data-item='edit-metadata']").click()

        const dialog = openDialog(page)
        await expect(field(dialog, 'title')).toHaveValue('Loose Song')
        await expect(field(dialog, 'artist')).toHaveValue('Loose Artist')
        await expect(field(dialog, 'genre')).toBeEditable()
        await field(dialog, 'genre').fill('E2E Edited Genre')
        await field(dialog, 'year').fill('1999')
        await dialog.locator("[data-part='dialog-footer'] button[type='submit']").click()

        await expect(openDialog(page)).toHaveCount(0)
        const info = part(page, 'detail-info')
        await expect(info.locator("[data-field='genre'] [data-part='meta-value']")).toHaveText(
            'E2E Edited Genre',
        )
        await expect(info.locator("[data-field='year'] [data-part='meta-value']")).toHaveText(
            '1999',
        )
        await expect(part(page, 'detail-title')).toHaveText('Loose Song')
    } finally {
        await updateTrackMetadata(request, id, { genre: null, year: null })
        await updateLibrarySettings(request, { fileWatcherEnabled: true })
    }
})

test.describe('on the desktop', () => {
    test.skip(({ isMobile }) => isMobile, 'Desktop only')

    test('opens the editor from the context menu of a song', async ({ page }) => {
        await page.goto('/library/songs')

        await part(page, 'list-row').filter({ hasText: 'First Song' }).click({ button: 'right' })
        await page.locator("[data-part='context-menu'] [data-item='edit-metadata']").click()

        await expect(page).toHaveURL(/\/library\/tracks\/[0-9a-f-]+\?edit=metadata$/)
        const dialog = openDialog(page)
        await expect(field(dialog, 'title')).toHaveValue('First Song')
        await expect(field(dialog, 'genre')).toHaveValue('E2E Genre')

        await page.keyboard.press('Escape')

        await expect(openDialog(page)).toHaveCount(0)
        await expect(page).toHaveURL(/\/library\/tracks\/[0-9a-f-]+$/)
    })
})
