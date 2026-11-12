import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import {
    createUser,
    deleteUser,
    findAlbumId,
    findTrackId,
    uniqueSuffix,
    type TestUser,
} from './helpers/api'
import { homeUrl, profileTile } from './helpers/navigation'
import { emptyStorageState } from './helpers/storage'

// Favorites, ratings, "my music" and the home page with the synthetic test media of
// scripts/generate-media.sh: E2E Album holds First Song and Second Song. Favorites and ratings
// belong to a profile, so every test selects its own new profile and the tests can run in
// parallel without touching the collection of the built-in admin.
test.use({ storageState: emptyStorageState, locale: 'en-US' })

let user: TestUser

test.beforeEach(async ({ page, request }) => {
    user = await createUser(request, `Collector ${uniqueSuffix()}`)
    await page.goto('/profiles')
    await profileTile(page, user.name).click()
    await expect(page).toHaveURL(homeUrl)
})

test.afterEach(async ({ request }) => {
    await deleteUser(request, user.id)
})

function part(page: Page, name: string): Locator {
    return page.locator(`[data-part='app'] [data-part='${name}']`)
}

function row(page: Page, title: string): Locator {
    return part(page, 'list-row').filter({ hasText: title })
}

function shelf(page: Page, item: string): Locator {
    return part(page, 'shelf').and(page.locator(`[data-item='${item}']`))
}

function tileTitles(container: Locator): Locator {
    return container.locator("[data-part='tile-title']")
}

/** A request with a new session of the profile of the test. */
async function userHeaders(request: APIRequestContext): Promise<Record<string, string>> {
    const session = await request.post('/api/v1/sessions', { data: { profileId: user.id } })
    expect(session.status()).toBe(201)
    return { Authorization: `Bearer ${(await session.json()).token}` }
}

async function trackState(
    request: APIRequestContext,
    id: string,
): Promise<{ isFavorite: boolean; rating: number }> {
    const response = await request.get(`/api/v1/tracks/${id}`, {
        headers: await userHeaders(request),
    })
    expect(response.status()).toBe(200)
    const track = await response.json()
    return { isFavorite: track.isFavorite, rating: track.rating }
}

test('marks a favorite in the song list and filters the library by "my music"', async ({
    page,
    request,
}) => {
    const firstSong = await findTrackId(request, 'First Song')
    await page.goto('/library/songs')

    const heart = row(page, 'First Song').locator(
        "[data-column='favorite'] [data-part='btn-favorite']",
    )
    await expect(heart).toHaveAttribute('aria-pressed', 'false')
    await heart.click()
    await expect(heart).toHaveAttribute('aria-pressed', 'true')
    await expect.poll(async () => (await trackState(request, firstSong)).isFavorite).toBe(true)
    // The click marks the favorite only, it does not open or play the song.
    await expect(page).toHaveURL(/\/library\/songs$/)

    await part(page, 'scope-switch').locator("[data-item='my-music']").click()
    await expect(page).toHaveURL(/[?&]myMusic=true/)
    await expect(part(page, 'list-row')).toHaveCount(1)
    await expect(row(page, 'First Song')).toBeVisible()

    await page.goto('/library/favorites')
    const songs = part(page, 'section').and(page.locator("[data-item='tracks']"))
    await expect(songs.locator("[data-part='list-row']")).toHaveCount(1)
    await expect(songs.locator("[data-part='list-row']")).toContainText('First Song')

    await songs.locator("[data-part='btn-favorite']").click()
    await expect.poll(async () => (await trackState(request, firstSong)).isFavorite).toBe(false)
})

test('rates a song in the song list', async ({ page, request, isMobile }) => {
    test.skip(isMobile, 'The song list shows no rating column on small screens.')
    const firstSong = await findTrackId(request, 'First Song')
    await page.goto('/library/songs')

    const rating = row(page, 'First Song').locator("[data-column='rating'] [data-part='rating']")
    await expect(rating).toHaveAttribute('data-state', 'unrated')
    await rating.locator("[data-part='rating-star']").nth(3).click()

    await expect(rating).toHaveAttribute('data-value', '4')
    await expect.poll(async () => (await trackState(request, firstSong)).rating).toBe(4)
    await expect(page).toHaveURL(/\/library\/songs$/)

    // The star of the current rating removes it.
    await rating.locator("[data-part='rating-star']").nth(3).click()
    await expect(rating).toHaveAttribute('data-state', 'unrated')
    await expect.poll(async () => (await trackState(request, firstSong)).rating).toBe(0)
})

test('the home page shows the favorites and the top rated songs of the profile', async ({
    page,
    request,
}) => {
    // A new profile has no favorites and no ratings, only the songs of the library.
    await expect(shelf(page, 'recently-added')).toBeVisible()
    await expect(shelf(page, 'favorites')).toHaveCount(0)
    await expect(shelf(page, 'top-rated')).toHaveCount(0)

    const secondSong = await findTrackId(request, 'Second Song')
    await page.goto(`/library/tracks/${secondSong}`)
    const view = part(page, 'view')
    const rating = view.locator("[data-part='rating']")
    await rating.locator("[data-part='rating-star']").nth(4).click()
    await expect(rating).toHaveAttribute('data-value', '5')
    const favorite = view.locator("[data-part='btn-favorite'][data-item='favorite']")
    await favorite.click()
    await expect(favorite).toHaveAttribute('aria-pressed', 'true')
    await expect
        .poll(() => trackState(request, secondSong))
        .toEqual({ isFavorite: true, rating: 5 })

    const album = await findAlbumId(request, 'E2E Album')
    await page.goto(`/library/albums/${album}`)
    const albumFavorite = part(page, 'view').locator(
        "[data-part='btn-favorite'][data-item='favorite']",
    )
    await albumFavorite.click()
    await expect(albumFavorite).toHaveAttribute('aria-pressed', 'true')
    // The button shows the favorite at once; the home page needs it saved.
    await expect
        .poll(async () => {
            const response = await request.get(`/api/v1/albums/${album}`, {
                headers: await userHeaders(request),
            })
            return (await response.json()).isFavorite
        })
        .toBe(true)

    await page.goto('/')
    await expect(part(page, 'view')).toHaveAttribute('data-view', 'home')
    // The newest favorite first.
    await expect(tileTitles(shelf(page, 'favorites'))).toHaveText(['E2E Album', 'Second Song'])
    await expect(tileTitles(shelf(page, 'top-rated'))).toHaveText(['Second Song'])

    await shelf(page, 'top-rated').locator("[data-part='shelf-actions'] a").click()
    await expect(page).toHaveURL(/\/library\/songs\?sort=Rating&myMusic=true$/)
    // "My music" holds the rated song and, through the favorite album, First Song as well.
    await expect(part(page, 'list-row')).toHaveCount(2)
    await expect(part(page, 'list-row').first()).toContainText('Second Song')
})
