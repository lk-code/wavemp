import { expect, test } from '@playwright/test'
import { builtInAdminTile } from './helpers/navigation'
import { emptyStorageState } from './helpers/storage'

test('start page opens the library and shows a healthy server', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveTitle('WaveMP')
    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(page.locator("[data-part='app']")).toBeVisible()
    await expect(page.locator("[data-part='server-status']")).toHaveAttribute(
        'data-state',
        'healthy',
    )
})

test('unknown client routes still load the web app and show the not found page', async ({
    page,
}) => {
    await page.goto('/some/client/route')

    await expect(page).toHaveTitle('WaveMP')
    await expect(page.locator("[data-part='view'][data-view='not-found']")).toBeVisible()
})

test.describe('without a selected profile', () => {
    test.use({ storageState: emptyStorageState })

    test('the start page shows the profile selection', async ({ page }) => {
        await page.goto('/')

        await expect(page).toHaveURL(/\/profiles$/)
        await expect(page.locator("[data-part='profile-select']")).toBeVisible()
        await expect(page.locator("[data-part='app']")).toHaveCount(0)
    })

    test('deep links open after the profile selection', async ({ page }) => {
        await page.goto('/now-playing')

        await expect(page).toHaveURL(/\/profiles\?redirect=(%2F|\/)now-playing$/)

        await builtInAdminTile(page).click()

        await expect(page).toHaveURL(/\/now-playing$/)
        await expect(page.locator("[data-part='view'][data-view='now-playing']")).toBeVisible()
    })
})
