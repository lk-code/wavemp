import { expect, test as setup } from '@playwright/test'
import { builtInAdminTile, navigationLabels } from './helpers/navigation'
import { adminStorageState } from './helpers/storage'

// Selects the built-in admin profile once; the tests reuse its session (storageState).
setup('select the built-in admin profile', async ({ page }) => {
    await page.goto('/')
    await expect(page).toHaveURL(/\/profiles$/)

    await builtInAdminTile(page).click()
    await expect(page).toHaveURL(/\/library\/songs$/)

    // The tests expect English texts, also if a former run changed the language of the profile.
    await page.goto('/settings')
    await page.locator("[data-part='select']").selectOption('en')
    await expect(page.locator('html')).toHaveAttribute('lang', 'en')
    expect(await navigationLabels(page)).toEqual(['Now Playing', 'Library', 'Playlists'])

    await page.context().storageState({ path: adminStorageState })
})
