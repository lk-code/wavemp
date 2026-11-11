import { expect, test } from '@playwright/test'

// The component overview exists only in the development build (npm run dev).

test('the component overview is not part of the production build', async ({ page, request }) => {
    await page.goto('/dev/components')

    await expect(page.locator("[data-part='view'][data-view='not-found']")).toBeVisible()
    await expect(page.locator("[data-part='dev-components']")).toHaveCount(0)

    // Neither the service worker precaches a chunk of it.
    const serviceWorker = await request.get('/sw.js')
    expect(serviceWorker.status()).toBe(200)
    expect(await serviceWorker.text()).not.toContain('DevComponents')
})
