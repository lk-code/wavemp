import { expect, test } from '@playwright/test'
import { resetLibrarySettings } from './helpers/api'
import { adminNavItem } from './helpers/navigation'

// The library page of the administration on the desktop and on the smartphone.

test('shows the navigation, the scan and the settings without horizontal scrolling', async ({
    page,
}) => {
    await page.goto('/admin/library')

    await expect(page.locator("[data-view='admin-library']")).toBeVisible()
    await expect(adminNavItem(page, 'admin-library')).toBeVisible()
    await expect(adminNavItem(page, 'admin-jobs')).toBeVisible()
    await expect(page.locator("[data-part='scan-status'] button")).toBeVisible()
    await expect(page.locator("form[data-part='form']")).toBeVisible()

    const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )
    expect(overflow).toBeLessThanOrEqual(0)
})

test('shows the jobs page without horizontal scrolling', async ({ page }) => {
    await page.goto('/admin/jobs')

    await expect(page.locator("[data-part='job-item'][data-item='library-scan']")).toBeVisible()
    const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    )
    expect(overflow).toBeLessThanOrEqual(0)
})

test.describe('settings', () => {
    test.beforeEach(({}, testInfo) => {
        test.skip(testInfo.project.name !== 'chromium', 'Settings are changed on one project only.')
    })

    test.afterEach(async ({ request }) => {
        await resetLibrarySettings(request)
    })

    test('saves the scan interval', async ({ page }) => {
        await page.goto('/admin/library')
        const interval = page.getByLabel('Scan the library')
        await expect(interval).toHaveValue('360')

        await interval.selectOption('720')
        await page.getByRole('button', { name: 'Save' }).click()

        await expect(page.locator("[data-part='alert'][data-state='success']")).toHaveText(
            'The settings are saved.',
        )
        await page.reload()
        await expect(page.getByLabel('Scan the library')).toHaveValue('720')
        await adminNavItem(page, 'admin-jobs').click()
        await expect(page.locator("[data-part='job-meta']")).toContainText('Every 720 minutes')
    })
})
