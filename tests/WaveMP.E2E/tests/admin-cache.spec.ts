import { expect, test } from '@playwright/test'
import { adminHeaders, findAlbumId, resetCacheSettings } from './helpers/api'
import { adminNavItem } from './helpers/navigation'

// The cache page of the administration on the desktop and on the smartphone.

test('shows the usage and the settings of the cache without horizontal scrolling', async ({
    page,
}) => {
    await page.goto('/admin/cache')

    await expect(page.locator("[data-view='admin-cache']")).toBeVisible()
    await expect(adminNavItem(page, 'admin-cache')).toHaveAttribute('aria-current', 'page')
    await expect(page.locator("[data-part='cache-usage'] [data-part='meter']")).toBeVisible()
    await expect(page.locator("[data-part='cache-usage'] [data-item='covers']")).toBeVisible()
    await expect(page.getByLabel('Maximum size')).toBeVisible()

    const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )
    expect(overflow).toBeLessThanOrEqual(0)
})

test.describe('changes', () => {
    test.beforeEach(({}, testInfo) => {
        test.skip(testInfo.project.name !== 'chromium', 'The cache is changed on one project only.')
    })

    test('clears the cache after asking', async ({ page, request }) => {
        // A scaled cover, so that the cache holds at least one file.
        const album = await findAlbumId(request, 'E2E Album')
        const cover = await request.get(`/api/v1/albums/${album}/cover?size=1000`, {
            headers: await adminHeaders(request),
        })
        expect(cover.status()).toBe(200)

        await page.goto('/admin/cache')
        const clear = page.locator("[data-part='cache-usage'] [data-part='button']")
        await expect(clear).toBeEnabled()

        await clear.click()
        const dialog = page.locator("[data-part='dialog']")
        await expect(dialog.locator("[data-part='dialog-title']")).toHaveText('Clear cache')
        await dialog.getByRole('button', { name: 'Clear', exact: true }).click()

        await expect(page.locator("[data-part='alert'][data-state='success']")).toHaveText(
            'The cache is empty.',
        )
        await expect(dialog).toBeHidden()
    })

    test('saves the size limit', async ({ page, request }) => {
        try {
            await page.goto('/admin/cache')
            const maxSize = page.getByLabel('Maximum size')
            await expect(maxSize).toHaveValue('2000')

            await maxSize.selectOption('5000')
            await page.getByRole('button', { name: 'Save' }).click()

            await expect(page.locator("[data-part='alert'][data-state='success']")).toHaveText(
                'The settings are saved.',
            )
            await expect(
                page.locator("[data-part='cache-usage'] > [data-part='label']").first(),
            ).toContainText('5 GB')
            await page.reload()
            await expect(page.getByLabel('Maximum size')).toHaveValue('5000')
        } finally {
            await resetCacheSettings(request)
        }
    })
})
