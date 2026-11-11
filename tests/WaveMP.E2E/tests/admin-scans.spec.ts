import { expect, test } from '@playwright/test'
import { latestJobRunId, latestScan, waitForIdleScan } from './helpers/api'
import { adminNavItem, chooseFromProfileMenu } from './helpers/navigation'

// Scans and jobs of the administration with the session of the built-in admin. Only on the
// desktop: scans of the whole media folder must not run from two projects at the same time.
// With E2E_EXPECTED_TRACKS (the CI creates the media with scripts/generate-media.sh), the result
// of the scan is checked as well.

test.describe.configure({ mode: 'serial' })

const expectedTracks = process.env.E2E_EXPECTED_TRACKS
    ? Number(process.env.E2E_EXPECTED_TRACKS)
    : undefined

test.beforeEach(async ({ request }, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium', 'Scans run on the desktop project only.')
    await waitForIdleScan(request)
})

test('scans the media folder from the library page', async ({ page, request }) => {
    test.setTimeout(180_000)
    await page.goto('/library/songs')
    await chooseFromProfileMenu(page, 'administration')
    await expect(page).toHaveURL(/\/admin\/library$/)
    await expect(adminNavItem(page, 'admin-library')).toHaveAttribute('aria-current', 'page')

    const status = page.locator("[data-part='scan-status']")
    await expect(status).toHaveAttribute('data-state', 'idle')
    const before = await latestScan(request)

    await status.getByRole('button', { name: 'Scan now' }).click()

    // The new scan appears, runs and ends; its result is the last scan.
    await expect
        .poll(async () => (await latestScan(request))?.id, { timeout: 30_000 })
        .not.toBe(before?.id)
    await expect(status).toHaveAttribute('data-state', 'idle', { timeout: 120_000 })
    const scanSection = page.locator("[data-part='section']").first()
    await expect(scanSection.locator("[data-part='run-meta'] > [data-part='badge']")).toHaveText(
        'Succeeded',
    )
    await expect(scanSection.locator("[data-part='stat-list']")).toBeVisible()
    await expect(
        page.locator("[data-part='run-list'] [data-part='run-item']").first(),
    ).toBeVisible()

    const scan = await latestScan(request)
    expect(scan?.status).toBe('Succeeded')
    if (expectedTracks !== undefined) {
        expect(scan?.statistics?.tracksTotal).toBe(expectedTracks)
        await expect(
            scanSection.locator(
                "[data-part='stat-item'][data-item='tracksTotal'] [data-part='stat-value']",
            ),
        ).toHaveText(String(expectedTracks))
    }
})

test('runs the library scan from the jobs page', async ({ page, request }) => {
    test.setTimeout(180_000)
    await page.goto('/admin/library')
    await adminNavItem(page, 'admin-jobs').click()
    await expect(page).toHaveURL(/\/admin\/jobs$/)

    const job = page.locator("[data-part='job-item'][data-item='library-scan']")
    await expect(job.locator("[data-part='job-name']")).toHaveText('Library scan')
    await expect(job.locator("[data-part='job-meta']")).toContainText('Next run')
    await expect(job.locator("[data-part='job-meta']")).toContainText(/Every \d+ minutes/)
    const before = await latestJobRunId(request, 'library-scan')

    await job.getByRole('button', { name: 'Run now' }).click()

    await expect
        .poll(() => latestJobRunId(request, 'library-scan'), { timeout: 30_000 })
        .not.toBe(before)
    await waitForIdleScan(request)
    await expect(job).toHaveAttribute('data-state', 'idle', { timeout: 30_000 })
    expect((await latestScan(request))?.status).toBe('Succeeded')
})
