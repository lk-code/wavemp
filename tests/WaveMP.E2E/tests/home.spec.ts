import { expect, test } from '@playwright/test'

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
