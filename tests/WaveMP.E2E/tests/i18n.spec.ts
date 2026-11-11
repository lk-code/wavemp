import { expect, test, type Page } from '@playwright/test'

// Until a profile is selected, the browser language decides; English is the fallback.

async function navigationLabels(page: Page): Promise<string[]> {
    // Tabs on the desktop, bottom navigation on mobile: the same entries.
    const labels = page
        .locator(
            "[data-part='tab'] > [data-part='label'], [data-part='bottom-nav-item'] > [data-part='label']",
        )
        .filter({ visible: true })
    await expect(labels).toHaveCount(3)
    return labels.allTextContents()
}

test.describe('English browser', () => {
    test.use({ locale: 'en-US' })

    test('shows English texts', async ({ page }) => {
        await page.goto('/library/songs')

        await expect(page.locator('html')).toHaveAttribute('lang', 'en')
        expect(await navigationLabels(page)).toEqual(['Now Playing', 'Library', 'Playlists'])
        await expect(page.locator("[data-part='list-column'][data-column='title']")).toHaveText(
            'Title',
        )
        await expect(page.locator("[data-part='content-title']")).toHaveText('Songs')
    })
})

test.describe('German browser', () => {
    test.use({ locale: 'de-DE' })

    test('shows German texts and formats', async ({ page }) => {
        await page.goto('/library/songs')

        await expect(page.locator('html')).toHaveAttribute('lang', 'de')
        expect(await navigationLabels(page)).toEqual([
            'Aktuelle Wiedergabe',
            'Bibliothek',
            'Playlists',
        ])
        await expect(page.locator("[data-part='list-column'][data-column='title']")).toHaveText(
            'Titel',
        )
        await expect(page.locator("[data-part='content-title']")).toHaveText('Titel')
        await expect(
            page.locator("[data-part='list-group-header']").first(),
        ).toHaveText('Platzhalter-Album 1')
        await expect(
            page.locator("[data-part='volume'] [role='slider']"),
        ).toHaveAttribute('aria-valuetext', /^80\s%$/u)
    })
})

test.describe('Unsupported browser language', () => {
    test.use({ locale: 'fr-FR' })

    test('falls back to English', async ({ page }) => {
        await page.goto('/library/songs')

        await expect(page.locator('html')).toHaveAttribute('lang', 'en')
        expect(await navigationLabels(page)).toEqual(['Now Playing', 'Library', 'Playlists'])
    })
})
