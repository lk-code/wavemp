import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import { adminHeaders, findTrackId, uniqueSuffix } from './helpers/api'

// Playlists of the built-in admin profile with the synthetic test media of
// scripts/generate-media.sh: E2E Album (with cover.jpg) holds First Song and Second Song.
// Every test uses its own playlist name, so the tests can run in parallel.

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function openDialog(page: Page): Locator {
    return page.locator("[data-part='dialog'][open]")
}

function rowTitles(page: Page): Locator {
    return part(page, 'list-row').locator("[data-column='title'] > [data-part='label']")
}

interface PlaylistSummary {
    id: string
    name: string
}

async function findPlaylist(
    request: APIRequestContext,
    name: string,
): Promise<PlaylistSummary | undefined> {
    const response = await request.get(
        `/api/v1/me/playlists?limit=100&q=${encodeURIComponent(name)}`,
        {
            headers: await adminHeaders(request),
        },
    )
    expect(response.status()).toBe(200)
    return ((await response.json()).items as PlaylistSummary[]).find(
        (playlist) => playlist.name === name,
    )
}

async function createPlaylist(
    request: APIRequestContext,
    name: string,
    trackIds: string[],
): Promise<string> {
    const response = await request.post('/api/v1/me/playlists', {
        data: { name, trackIds },
        headers: await adminHeaders(request),
    })
    expect(response.status()).toBe(201)
    return (await response.json()).id
}

async function deletePlaylist(request: APIRequestContext, name: string): Promise<void> {
    const playlist = await findPlaylist(request, name)
    if (playlist) {
        const response = await request.delete(`/api/v1/me/playlists/${playlist.id}`, {
            headers: await adminHeaders(request),
        })
        expect([204, 404]).toContain(response.status())
    }
}

async function entryTitles(request: APIRequestContext, id: string): Promise<string[]> {
    const response = await request.get(`/api/v1/me/playlists/${id}/tracks?limit=100`, {
        headers: await adminHeaders(request),
    })
    expect(response.status()).toBe(200)
    return ((await response.json()).items as { track: { title: string } }[]).map(
        (entry) => entry.track.title,
    )
}

async function addFromSongMenu(page: Page, title: string, playlist: string): Promise<void> {
    await part(page, 'list-row').filter({ hasText: title }).first().click({ button: 'right' })
    await page.locator("[data-part='context-menu'] [data-item='add-to-playlist']").click()
    const dialog = openDialog(page)
    await expect(dialog.locator("[data-part='dialog-title']")).toHaveText('Add to playlist')
    await dialog
        .locator("[data-part='playlist-choice']")
        .filter({ has: page.locator("[data-part='label']", { hasText: playlist }) })
        .click()
    await expect(openDialog(page)).toHaveCount(0)
}

test.describe('on the desktop', () => {
    test.skip(({ isMobile }) => isMobile, 'The context menu of a row opens with a right click.')

    test('creates a playlist, adds songs from their menu, reorders, plays and deletes it', async ({
        page,
        request,
    }) => {
        const name = `E2E Playlist ${uniqueSuffix()}`
        try {
            // Create
            await page.goto('/playlists')
            await part(page, 'view-actions').locator("[data-item='create-playlist']").click()
            const dialog = openDialog(page)
            await expect(dialog.locator("[data-part='dialog-title']")).toHaveText('New playlist')
            await dialog.getByLabel('Name', { exact: true }).fill(name)
            await dialog.locator("button[type='submit']").click()

            await expect(page).toHaveURL(/\/playlists\/[0-9a-f-]+$/)
            const id = page.url().split('/').pop()!
            await expect(part(page, 'detail-title')).toHaveText(name)
            await expect(part(page, 'empty-state')).toBeVisible()

            // Add two songs, one of them twice
            await page.goto('/library/songs')
            await addFromSongMenu(page, 'First Song', name)
            await addFromSongMenu(page, 'Second Song', name)
            await addFromSongMenu(page, 'First Song', name)
            expect(await entryTitles(request, id)).toEqual([
                'First Song',
                'Second Song',
                'First Song',
            ])

            // The sidebar lists the playlist and leads to it.
            await part(page, 'sidebar').getByRole('treeitem', { name }).click()
            await expect(page).toHaveURL(new RegExp(`/playlists/${id}$`))
            await expect(rowTitles(page)).toHaveText(['First Song', 'Second Song', 'First Song'])
            // Both songs come from the same album, so its cover stands for the playlist.
            await expect(
                part(page, 'detail-header').locator("[data-part='playlist-cover']"),
            ).toHaveAttribute('data-count', '1')

            // Move the second entry up with its menu
            await part(page, 'list-row').nth(1).click({ button: 'right' })
            await page.locator("[data-part='context-menu'] [data-item='move-up']").click()
            await expect(rowTitles(page)).toHaveText(['Second Song', 'First Song', 'First Song'])
            await expect
                .poll(() => entryTitles(request, id))
                .toEqual(['Second Song', 'First Song', 'First Song'])

            // Remove the last entry
            await part(page, 'list-row').nth(2).click({ button: 'right' })
            await page
                .locator("[data-part='context-menu'] [data-item='remove-from-playlist']")
                .click()
            await expect(rowTitles(page)).toHaveText(['Second Song', 'First Song'])
            await expect.poll(() => entryTitles(request, id)).toEqual(['Second Song', 'First Song'])

            // Play; pausing right away keeps the short song from ending during the test.
            await part(page, 'detail-actions').locator("[data-item='play']").click()
            await part(page, 'btn-play').click()
            await expect(part(page, 'btn-play')).toHaveAttribute('data-state', 'paused')
            await expect(part(page, 'now-playing-title')).toHaveText('Second Song')

            // Delete
            await part(page, 'detail-actions').locator("[data-item='delete']").click()
            await expect(openDialog(page)).toContainText(name)
            await openDialog(page)
                .locator("[data-part='dialog-footer'] button", { hasText: 'Delete' })
                .click()
            await expect(page).toHaveURL(/\/playlists$/)
            await expect(part(page, 'tile').filter({ hasText: name })).toHaveCount(0)
            expect(await findPlaylist(request, name)).toBeUndefined()
        } finally {
            await deletePlaylist(request, name)
        }
    })
})

test('shows a playlist among the tiles and renames it', async ({ page, request }) => {
    const name = `E2E Tiles ${uniqueSuffix()}`
    const renamed = `${name} renamed`
    const first = await findTrackId(request, 'First Song')
    await createPlaylist(request, name, [first, first])
    try {
        await page.goto('/playlists')
        await expect(part(page, 'tile').filter({ hasText: 'Recently Played' })).toBeVisible()
        const tile = part(page, 'tile').filter({ hasText: name })
        await expect(tile).toHaveAttribute('data-kind', 'playlist')
        await tile.click()

        await expect(part(page, 'detail-title')).toHaveText(name)
        await expect(rowTitles(page)).toHaveText(['First Song', 'First Song'])

        await part(page, 'detail-actions').locator("[data-item='rename']").click()
        const dialog = openDialog(page)
        await expect(dialog.locator("[data-part='dialog-title']")).toHaveText('Rename playlist')
        await dialog.getByLabel('Name', { exact: true }).fill(renamed)
        await dialog.locator("button[type='submit']").click()

        await expect(openDialog(page)).toHaveCount(0)
        await expect(part(page, 'detail-title')).toHaveText(renamed)
        expect(await findPlaylist(request, renamed)).toBeDefined()
    } finally {
        await deletePlaylist(request, name)
        await deletePlaylist(request, renamed)
    }
})
