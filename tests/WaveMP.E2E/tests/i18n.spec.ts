import { expect, test } from '@playwright/test'
import { navigationLabels } from './helpers/navigation'
import { emptyStorageState } from './helpers/storage'

// Until a profile is selected, the browser language decides (English as fallback); afterwards the
// language of the profile.

const browserLanguages = [
    { locale: 'en-US', lang: 'en', title: "Who's listening?" },
    { locale: 'de-DE', lang: 'de', title: 'Wer hört zu?' },
    { locale: 'fr-FR', lang: 'en', title: "Who's listening?" },
]

for (const { locale, lang, title } of browserLanguages) {
    test.describe(`profile selection in a browser with ${locale}`, () => {
        test.use({ storageState: emptyStorageState, locale })

        test(`shows the language ${lang}`, async ({ page }) => {
            await page.goto('/')

            await expect(page).toHaveURL(/\/profiles$/)
            await expect(page.locator('html')).toHaveAttribute('lang', lang)
            await expect(page.locator("[data-part='profile-select-title']")).toHaveText(title)
        })
    })
}

test.describe('selected profile in a German browser', () => {
    test.use({ locale: 'de-DE' })

    test('uses the language of the profile', async ({ page }) => {
        await page.goto('/library/songs')

        await expect(page.locator('html')).toHaveAttribute('lang', 'en')
        expect(await navigationLabels(page)).toEqual(['Home', 'Now Playing', 'Library', 'Playlists'])
        await expect(page.locator("[data-part='list-column'][data-column='title']")).toHaveText(
            'Title',
        )
        await expect(page.locator("[data-part='content-title']")).toHaveText('Songs')
    })
})
