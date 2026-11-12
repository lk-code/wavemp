import { expect, test, type Locator, type Page } from '@playwright/test'

// Fixed mobile base (below 64rem): bottom navigation, drawer, mini player and full-screen player.
test.skip(({ isMobile }) => !isMobile, 'Mobile layout only')

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

async function box(locator: Locator) {
    const result = await locator.boundingBox()
    expect(result).not.toBeNull()
    return result!
}

test('shows the bottom navigation instead of tabs, history and sidebar', async ({ page }) => {
    await page.goto('/library/songs')
    const viewport = page.viewportSize()!

    const bottomNav = part(page, 'bottom-nav')
    await expect(bottomNav).toBeVisible()
    await expect(part(page, 'bottom-nav-item')).toHaveCount(4)
    const navBox = await box(bottomNav)
    expect(navBox.y + navBox.height).toBeCloseTo(viewport.height, 0)
    await expect(part(page, 'bottom-nav-item').filter({ hasText: 'Library' })).toHaveAttribute(
        'aria-current',
        'page',
    )

    for (const name of ['tabs', 'nav-history', 'sidebar', 'breadcrumb', 'mode-switch']) {
        await expect(part(page, name), name).toBeHidden()
    }
    for (const name of [
        'btn-menu',
        'logo',
        'server-status',
        'btn-profile',
        'search',
        'content-title',
    ]) {
        await expect(part(page, name), name).toBeVisible()
    }
})

test('opens the sidebar as a drawer', async ({ page }) => {
    await page.goto('/library/songs')
    const sidebar = part(page, 'sidebar')
    const menu = part(page, 'btn-menu')

    await expect(menu).toHaveAttribute('aria-expanded', 'false')
    await menu.click()

    await expect(sidebar).toHaveAttribute('data-state', 'open')
    await expect(sidebar).toBeVisible()
    await expect(part(page, 'tree')).toBeVisible()
    await expect(menu).toHaveAttribute('aria-expanded', 'true')

    const viewport = page.viewportSize()!
    await part(page, 'sidebar-backdrop').click({
        position: { x: viewport.width - 10, y: viewport.height / 2 },
    })

    await expect(sidebar).toHaveAttribute('data-state', 'closed')
    await expect(sidebar).toBeHidden()

    // Choosing an entry navigates and closes the drawer.
    await menu.click()
    await page.getByRole('treeitem', { name: 'Album', exact: true }).click()
    await expect(page).toHaveURL(/\/library\/albums$/)
    await expect(sidebar).toBeHidden()
})

test('shows the transport as mini player above the bottom navigation', async ({ page }) => {
    await page.goto('/library/songs')

    const transport = await box(part(page, 'transport'))
    const bottomNav = await box(part(page, 'bottom-nav'))
    expect(transport.y + transport.height).toBeCloseTo(bottomNav.y, 0)

    await expect(part(page, 'now-playing-mini')).toBeVisible()
    await expect(part(page, 'btn-play')).toBeVisible()
    for (const name of ['btn-shuffle', 'btn-repeat', 'btn-stop', 'btn-prev', 'btn-next']) {
        await expect(part(page, name), name).toBeHidden()
    }
    await expect(part(page, 'btn-collapse')).toBeHidden()
})

test('expands the mini player to a full-screen player', async ({ page }) => {
    await page.goto('/library/songs')
    const viewport = page.viewportSize()!
    const transport = part(page, 'transport')

    await part(page, 'now-playing-mini').click()

    await expect(transport).toHaveAttribute('data-state', /\bexpanded\b/)
    const expanded = await box(transport)
    expect(expanded.x).toBe(0)
    expect(expanded.y).toBe(0)
    expect(expanded.width).toBeCloseTo(viewport.width, 0)
    expect(expanded.height).toBeCloseTo(viewport.height, 0)
    for (const name of ['btn-collapse', 'btn-shuffle', 'btn-repeat', 'btn-prev', 'btn-next']) {
        await expect(part(page, name), name).toBeVisible()
    }
    await expect(part(page, 'volume')).toBeVisible()

    await part(page, 'btn-collapse').click()

    await expect(transport).not.toHaveAttribute('data-state', /\bexpanded\b/)
    await expect(part(page, 'btn-collapse')).toBeHidden()
})

test('keeps the mobile base when a theme tries to change it', async ({ page }) => {
    await page.goto('/library/songs')
    const viewport = page.viewportSize()!

    // Theme rules have no cascade layer; even with !important they lose against the mobile base.
    await page.addStyleTag({
        content: `[data-part=app] { grid-template-areas: "content" !important; }
            [data-part=tabs] { display: flex !important; }
            [data-part=bottom-nav] { display: none !important; }`,
    })

    const bottomNav = part(page, 'bottom-nav')
    await expect(bottomNav).toBeVisible()
    const navBox = await box(bottomNav)
    expect(navBox.y + navBox.height).toBeCloseTo(viewport.height, 0)
    await expect(part(page, 'tabs')).toBeHidden()
})
