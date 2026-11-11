import { expect, test } from '@playwright/test'
import { deleteUser, findUserByName, uniqueSuffix } from './helpers/api'
import { adminNavItem, chooseFromProfileMenu } from './helpers/navigation'

// Profile administration with the session of the built-in admin.

let name: string

test.beforeEach(() => {
    name = `Erika ${uniqueSuffix()} Muster`
})

test.afterEach(async ({ request }) => {
    const user = await findUserByName(request, name)
    if (user) {
        await deleteUser(request, user.id)
    }
})

test('creates, edits and deletes a profile', async ({ page }) => {
    await page.goto('/library/songs')
    await chooseFromProfileMenu(page, 'administration')
    await expect(page).toHaveURL(/\/admin\/library$/)
    await adminNavItem(page, 'admin-users').click()
    await expect(page).toHaveURL(/\/admin\/users$/)
    await expect(adminNavItem(page, 'admin-users')).toHaveAttribute('aria-current', 'page')

    const builtIn = page.locator("[data-part='user-item'][data-state~='built-in']")
    await expect(builtIn.locator("[data-part='user-actions'] button").nth(1)).toBeDisabled()

    // Create: the initials come from the name.
    await page.locator("[data-part='view-actions'] button").click()
    const dialog = page.locator("[data-part='dialog'][open]")
    await expect(dialog.locator("[data-part='dialog-title']")).toHaveText('Add profile')
    await dialog.getByLabel('Name', { exact: true }).fill(name)
    await dialog.locator("button[type='submit']").click()

    const item = page.locator("[data-part='user-item']").filter({
        has: page.locator("[data-part='user-name']", { hasText: name }),
    })
    await expect(item.locator("[data-part='avatar-initials']")).toHaveText('EM')
    await expect(item.locator("[data-part='badge']").first()).toHaveText('User')

    // Edit: own initials and another color.
    await item.locator("[data-part='user-actions'] button").first().click()
    await expect(dialog.locator("[data-part='dialog-title']")).toHaveText('Edit profile')
    await dialog.getByRole('textbox', { name: 'Initials' }).fill('XY')
    await dialog.getByRole('radio', { name: 'Green' }).click()
    await dialog.locator("button[type='submit']").click()

    await expect(item.locator("[data-part='avatar-initials']")).toHaveText('XY')
    await expect(item.locator("[data-part='avatar']")).toHaveAttribute(
        'style',
        /--wmp-profile-color:\s*#2e7d32/,
    )

    // Delete after the confirmation.
    await item.locator("[data-part='user-actions'] button").nth(1).click()
    await expect(dialog).toContainText(`Delete the profile ${name}?`)
    await dialog.locator("[data-part='dialog-footer'] button", { hasText: 'Delete' }).click()

    await expect(item).toHaveCount(0)
})

test('shows a taken name at the name field', async ({ page }) => {
    await page.goto('/admin/users')

    await page.locator("[data-part='view-actions'] button").click()
    const dialog = page.locator("[data-part='dialog'][open]")
    await dialog.getByLabel('Name', { exact: true }).fill('ADMIN')
    await dialog.locator("button[type='submit']").click()

    await expect(dialog.locator("[data-part='field-error']")).toHaveText(
        'A profile with this name already exists.',
    )
})
