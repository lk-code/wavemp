import { expect, test, type Locator, type Page } from '@playwright/test'

// Desktop layout of the theme Midnight (from 64rem on). The tests address the DOM only through
// the data-part attributes of the theme API.
test.skip(({ isMobile }) => isMobile, 'Desktop layout only')

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

async function box(locator: Locator) {
    const result = await locator.boundingBox()
    expect(result).not.toBeNull()
    return result!
}

async function maskOf(icon: Locator): Promise<string> {
    return icon
        .locator("[data-part='icon-glyph']")
        .evaluate((element) => {
            const style = getComputedStyle(element)
            return style.maskImage || style.getPropertyValue('-webkit-mask-image')
        })
}

test('shows all desktop areas and hides the mobile ones', async ({ page }) => {
    await page.goto('/library/songs')

    const visible = [
        'header',
        'nav-history',
        'btn-back',
        'btn-forward',
        'tabs',
        'server-status',
        'btn-profile',
        'toolbar',
        'breadcrumb',
        'view-switch',
        'sort',
        'search',
        'sidebar',
        'tree',
        'content',
        'list',
        'list-header',
        'transport',
        'seek',
        'now-playing-mini',
        'transport-controls',
        'btn-shuffle',
        'btn-repeat',
        'btn-stop',
        'btn-prev',
        'btn-play',
        'btn-next',
        'btn-mute',
        'volume',
        'mode-switch',
        'btn-compact',
    ]
    for (const name of visible) {
        await expect(part(page, name), name).toBeVisible()
    }
    await expect(part(page, 'tab')).toHaveCount(4)
    await expect(part(page, 'tree-item').first()).toBeVisible()
    await expect(part(page, 'list-group').first()).toBeVisible()
    await expect(part(page, 'list-row').first()).toBeVisible()

    for (const name of ['bottom-nav', 'btn-menu', 'btn-collapse', 'sidebar-backdrop']) {
        await expect(part(page, name), name).toBeHidden()
    }
})

test('arranges the areas like a desktop media player', async ({ page }) => {
    await page.goto('/library/songs')
    const viewport = page.viewportSize()!

    const header = await box(part(page, 'header'))
    const toolbar = await box(part(page, 'toolbar'))
    const sidebar = await box(part(page, 'sidebar'))
    const content = await box(part(page, 'content'))
    const transport = await box(part(page, 'transport'))
    const seek = await box(part(page, 'seek'))
    const controls = await box(part(page, 'transport-controls'))

    expect(header.y).toBe(0)
    expect(toolbar.y).toBeGreaterThanOrEqual(header.y + header.height - 1)
    expect(sidebar.y).toBeGreaterThanOrEqual(toolbar.y + toolbar.height - 1)
    expect(sidebar.x + sidebar.width).toBeLessThanOrEqual(content.x + 1)
    expect(transport.y + transport.height).toBeCloseTo(viewport.height, 0)
    expect(seek.y + seek.height).toBeLessThanOrEqual(controls.y + 1)
    // The transport controls sit in the middle of the bar.
    expect(controls.x + controls.width / 2).toBeCloseTo(viewport.width / 2, -1)
})

test('loads the theme Midnight with its graphics and icons', async ({ page }) => {
    const failed: string[] = []
    page.on('response', (response) => {
        const path = new URL(response.url()).pathname
        if (/^\/(themes|icons|brand)\//.test(path) && response.status() >= 400) {
            failed.push(`${response.status()} ${path}`)
        }
    })

    await page.goto('/library/songs')

    await expect(page.locator('html')).toHaveAttribute('data-theme', 'midnight')
    await expect(page.locator('html')).toHaveAttribute('data-theme-api', '3')
    await expect(page.locator('link#wavemp-theme')).toHaveAttribute(
        'href',
        '/themes/midnight/theme.css',
    )
    await expect(page.locator('link#wavemp-theme')).toHaveCount(1)
    await expect(part(page, 'header')).toHaveCSS('background-image', /linear-gradient/)
    await expect(part(page, 'btn-back')).toHaveCSS('background-image', /glass-button.*\.svg/)
    await expect(part(page, 'btn-play')).toHaveCSS('background-image', /play-button.*\.svg/)
    await page.waitForLoadState('networkidle')
    expect(failed).toEqual([])
})

test('uses the icon style of each area', async ({ page }) => {
    await page.goto('/library/songs')

    expect(await maskOf(part(page, 'btn-play'))).toContain('/icons/phosphor/fill/play-fill.svg')
    expect(await maskOf(part(page, 'btn-back'))).toContain('/fill/arrow-left-fill.svg')
    expect(
        await maskOf(part(page, 'breadcrumb').locator("[data-icon='breadcrumb-root']")),
    ).toContain('/fill/music-notes-fill.svg')

    const library = page.getByRole('treeitem', { name: 'Library', exact: true })
    expect(await maskOf(library.locator("[data-icon='tree-library']"))).toContain(
        '/duotone/books-duotone.svg',
    )
    expect(await maskOf(library.locator("[data-part='tree-toggle']"))).toContain(
        '/regular/minus-square.svg',
    )
    await library.locator("[data-part='tree-toggle']").click()
    expect(await maskOf(library.locator("[data-part='tree-toggle']"))).toContain(
        '/regular/plus-square.svg',
    )
    expect(await maskOf(part(page, 'badge').first())).toContain('/regular/video-camera.svg')
})

test('navigates with the tabs and the history buttons', async ({ page }) => {
    await page.goto('/library/songs')
    const libraryTab = part(page, 'tab').filter({ hasText: 'Library' })
    const nowPlayingTab = part(page, 'tab').filter({ hasText: 'Now Playing' })

    await expect(libraryTab).toHaveAttribute('aria-current', 'page')
    await expect(part(page, 'btn-back')).toBeDisabled()

    await nowPlayingTab.click()

    await expect(page).toHaveURL(/\/now-playing$/)
    await expect(nowPlayingTab).toHaveAttribute('aria-current', 'page')
    await expect(libraryTab).not.toHaveAttribute('aria-current', 'page')
    await expect(part(page, 'view')).toHaveAttribute('data-view', 'now-playing')
    await expect(part(page, 'btn-back')).toBeEnabled()

    await part(page, 'btn-back').click()

    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(part(page, 'btn-forward')).toBeEnabled()
})

test('expands and collapses the tree and navigates to a section', async ({ page }) => {
    await page.goto('/library/songs')
    const library = page.getByRole('treeitem', { name: 'Library', exact: true })
    const album = page.getByRole('treeitem', { name: 'Album', exact: true })

    await expect(library).toHaveAttribute('aria-expanded', 'true')
    await expect(album).toBeVisible()
    await expect(page.getByRole('treeitem', { name: 'Songs', exact: true })).toHaveAttribute(
        'aria-current',
        'page',
    )

    await library.locator("[data-part='tree-toggle']").click()
    await expect(library).toHaveAttribute('aria-expanded', 'false')
    await expect(album).toBeHidden()

    await library.locator("[data-part='tree-toggle']").click()
    await expect(library).toHaveAttribute('aria-expanded', 'true')
    await expect(album).toBeVisible()

    await album.click()

    await expect(page).toHaveURL(/\/library\/albums$/)
    await expect(album).toHaveAttribute('aria-current', 'page')
    await expect(part(page, 'view')).toHaveAttribute('data-section', 'albums')
    await expect(part(page, 'tile-list')).toBeVisible()
})

test('shows the songs of the library grouped by album', async ({ page }) => {
    await page.goto('/library/songs')

    await expect(part(page, 'placeholder-notice')).toHaveCount(0)
    await expect(part(page, 'list')).not.toHaveAttribute('data-state', 'placeholder')
    const columns = part(page, 'list-column')
    await expect(columns).toHaveCount(6)
    expect(
        await columns.evaluateAll((elements) =>
            elements.map((element) => element.getAttribute('data-column')),
        ),
    ).toEqual(['number', 'title', 'duration', 'artist', 'rating', 'favorite'])

    const group = part(page, 'list-group').first()
    await expect(group.locator("[data-part='list-group-header']")).toHaveText('E2E Album')
    await expect(group.locator("[data-part='cover']")).toBeVisible()
    await expect(group.locator("[data-part='meta-item']")).toHaveCount(4)
    await expect(group.locator("[data-part='meta-item'][data-field='year']")).toContainText('2026')

    const rows = group.locator("[data-part='list-row']")
    await expect(rows.first().locator("[data-column='duration']")).toHaveText('0:02')
    await rows.first().click()
    await expect(rows.first()).toHaveAttribute('aria-selected', 'true')
    await page.keyboard.press('ArrowDown')
    await expect(rows.nth(1)).toHaveAttribute('aria-selected', 'true')
    await expect(rows.first()).toHaveAttribute('aria-selected', 'false')
})

test('operates volume and mute while nothing plays', async ({ page }) => {
    await page.goto('/library/songs')
    const volume = part(page, 'volume').getByRole('slider')
    const mute = part(page, 'btn-mute')

    await expect(part(page, 'transport')).toHaveAttribute('data-state', 'idle')
    await expect(part(page, 'btn-play')).toBeDisabled()
    await expect(part(page, 'seek').getByRole('slider')).toHaveAttribute('aria-disabled', 'true')

    await expect(volume).toHaveAttribute('aria-valuenow', '0.8')
    await volume.focus()
    await page.keyboard.press('ArrowLeft')
    await expect(volume).toHaveAttribute('aria-valuenow', '0.75')
    await expect(volume).toHaveAttribute('aria-valuetext', '75%')

    await expect(mute).toHaveAttribute('aria-pressed', 'false')
    await mute.click()
    await expect(mute).toHaveAttribute('aria-pressed', 'true')
    await expect(mute.locator("[data-part='icon']")).toHaveAttribute('data-icon', 'muted')

    const repeat = part(page, 'btn-repeat')
    await repeat.click()
    await repeat.click()
    await expect(repeat).toHaveAttribute('data-state', 'one')
    await expect(repeat.locator("[data-part='icon']")).toHaveAttribute('data-icon', 'repeat-one')
})

test('lets a theme rearrange the desktop layout', async ({ page }) => {
    await page.goto('/library/songs')

    // A rule without cascade layer, like every theme rule: the sidebar moves to the right.
    await page.addStyleTag({
        content: `@media (width >= 64rem) {
            [data-part='app'] {
                grid-template-areas: 'header header' 'toolbar toolbar' 'content sidebar' 'transport transport';
                grid-template-columns: minmax(0, 1fr) var(--wmp-sidebar-width);
            }
        }`,
    })

    const sidebar = await box(part(page, 'sidebar'))
    const content = await box(part(page, 'content'))
    expect(sidebar.x).toBeGreaterThan(content.x)
})
