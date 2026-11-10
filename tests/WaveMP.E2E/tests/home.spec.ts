import { expect, test } from '@playwright/test'

test('start page shows the app name and a healthy server', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveTitle('WaveMP')
    await expect(page.getByRole('heading', { level: 1, name: 'WaveMP' })).toBeVisible()
    await expect(page.getByTestId('health-status')).toHaveText('Healthy')
})

test('unknown client routes still load the web app', async ({ page }) => {
    await page.goto('/some/client/route')

    await expect(page).toHaveTitle('WaveMP')
})
