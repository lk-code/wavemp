import { expect, test } from '@playwright/test'
import { createUser, deleteUser, uniqueSuffix, type TestUser } from './helpers/api'
import {
    builtInAdminTile,
    chooseFromProfileMenu,
    navigationLabels,
    profileButton,
    profileTile,
} from './helpers/navigation'
import { emptyStorageState } from './helpers/storage'

// Selecting, remembering and switching profiles and the language per profile. These tests sign
// out, so they start without the shared admin session and select their profiles themselves.
test.use({ storageState: emptyStorageState, locale: 'en-US' })

let user: TestUser

test.beforeEach(async ({ request }) => {
    user = await createUser(request, `Erika ${uniqueSuffix()} Listener`)
})

test.afterEach(async ({ request }) => {
    await deleteUser(request, user.id)
})

test('selects a profile and remembers it', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/\/profiles$/)
    await expect(page.locator("[data-part='profile-select-title']")).toHaveText("Who's listening?")
    // The built-in admin steps back as soon as there are other profiles.
    await expect(builtInAdminTile(page)).toHaveAttribute('data-state', /\bsecondary\b/)

    await profileTile(page, user.name).click()

    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(profileButton(page).locator("[data-part='avatar-initials']")).toHaveText('EL')

    await page.reload()

    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(profileButton(page)).toBeVisible()
    await profileButton(page).click()
    await expect(page.locator("[data-part='context-menu'] [data-part='menu-item']")).toHaveText([
        'Settings',
        'Switch profile',
    ])
    await page.keyboard.press('Escape')

    // Profiles without the role admin never reach the profile administration.
    await page.goto('/admin/users')
    await expect(page).toHaveURL(/\/library\/songs$/)
})

test('switches the profile through the menu', async ({ page }) => {
    await page.goto('/profiles')
    await profileTile(page, user.name).click()
    await expect(page).toHaveURL(/\/library\/songs$/)

    await chooseFromProfileMenu(page, 'switch-profile')

    await expect(page).toHaveURL(/\/profiles$/)
    await expect(profileTile(page, user.name)).not.toHaveAttribute('data-state', /\bcurrent\b/)

    await builtInAdminTile(page).click()

    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(profileButton(page).locator("[data-part='avatar-initials']")).toHaveText('A')
    await profileButton(page).click()
    await expect(
        page.locator("[data-part='context-menu'] [data-item='manage-profiles']"),
    ).toBeVisible()
})

test('keeps the language per profile', async ({ page }) => {
    await page.goto('/profiles')
    await profileTile(page, user.name).click()
    await expect(page).toHaveURL(/\/library\/songs$/)

    await chooseFromProfileMenu(page, 'settings')
    await expect(page).toHaveURL(/\/settings$/)
    await page.locator("[data-part='select']").selectOption('de')

    await expect(page.locator('html')).toHaveAttribute('lang', 'de')
    expect(await navigationLabels(page)).toEqual(['Aktuelle Wiedergabe', 'Bibliothek', 'Playlists'])
    await expect(page.locator("[data-part='content-title']")).toHaveText('Einstellungen')

    await page.goto('/library/songs')
    await expect(page.locator("[data-part='list-column'][data-column='title']")).toHaveText('Titel')
    await expect(page.locator("[data-part='list-group-header']").first()).toHaveText(
        'Platzhalter-Album 1',
    )
    await expect(page.locator("[data-part='volume'] [role='slider']")).toHaveAttribute(
        'aria-valuetext',
        /^80\s%$/u,
    )

    await page.reload()
    await expect(page.locator('html')).toHaveAttribute('lang', 'de')

    // Before a profile is selected, the browser language applies again.
    await chooseFromProfileMenu(page, 'switch-profile')
    await expect(page).toHaveURL(/\/profiles$/)
    await expect(page.locator('html')).toHaveAttribute('lang', 'en')
    await expect(page.locator("[data-part='profile-select-title']")).toHaveText("Who's listening?")

    // The language is stored on the server: selecting the profile again brings it back.
    await profileTile(page, user.name).click()
    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(page.locator('html')).toHaveAttribute('lang', 'de')

    await chooseFromProfileMenu(page, 'switch-profile')
    await builtInAdminTile(page).click()
    await expect(page).toHaveURL(/\/library\/songs$/)
    await expect(page.locator('html')).toHaveAttribute('lang', 'en')
    expect(await navigationLabels(page)).toEqual(['Now Playing', 'Library', 'Playlists'])
})
