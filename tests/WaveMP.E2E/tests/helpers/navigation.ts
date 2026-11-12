import { expect, type Locator, type Page } from '@playwright/test'

/** Address of the home page, the start page after the profile selection. */
export const homeUrl = /^https?:\/\/[^/]+\/$/

/** Labels of the main navigation: tabs on the desktop, bottom navigation on mobile. */
export async function navigationLabels(page: Page): Promise<string[]> {
    const labels = page
        .locator(
            "[data-part='tab'] > [data-part='label'], [data-part='bottom-nav-item'] > [data-part='label']",
        )
        .filter({ visible: true })
    await expect(labels).toHaveCount(4)
    return labels.allTextContents()
}

/** The profile button in the header of the app. */
export function profileButton(page: Page): Locator {
    return page.locator("[data-part='app'] [data-part='btn-profile']")
}

/** Opens the profile menu and chooses an entry (settings, administration, switch-profile). */
export async function chooseFromProfileMenu(page: Page, item: string): Promise<void> {
    await profileButton(page).click()
    await page.locator(`[data-part='context-menu'] [data-item='${item}']`).click()
}

/** The tile of a profile on the profile selection. */
export function profileTile(page: Page, name: string): Locator {
    return page.locator("[data-part='profile-tile']").filter({
        has: page.locator("[data-part='label']", { hasText: name }),
    })
}

export function builtInAdminTile(page: Page): Locator {
    return page.locator("[data-part='profile-tile'][data-state~='built-in']")
}

/** The navigation of the administration. */
export function adminNavItem(page: Page, item: string): Locator {
    return page.locator(`[data-part='admin-nav'] [data-item='${item}']`)
}
