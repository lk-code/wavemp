import { expect, test } from '@playwright/test'

// Installable web app: manifest, icons and a service worker for the app shell only.
test.skip(
    ({ isMobile, browserName }) => isMobile || browserName !== 'chromium',
    'Desktop Chromium only',
)

/** Width and height from the IHDR chunk of a PNG file. */
function pngSize(data: Buffer): { width: number; height: number } {
    expect(data.subarray(1, 4).toString('ascii')).toBe('PNG')
    return { width: data.readUInt32BE(16), height: data.readUInt32BE(20) }
}

test('provides a web app manifest with icons', async ({ page, request }) => {
    await page.goto('/library/songs')
    await expect(page.locator('link[rel="manifest"]')).toHaveAttribute(
        'href',
        '/manifest.webmanifest',
    )

    const response = await request.get('/manifest.webmanifest')
    expect(response.status()).toBe(200)
    expect(response.headers()['content-type']).toContain('application/manifest+json')

    const manifest = await response.json()
    expect(manifest).toMatchObject({
        name: 'WaveMP',
        short_name: 'WaveMP',
        display: 'standalone',
        start_url: '/',
        scope: '/',
    })

    const icons = manifest.icons as { src: string; sizes: string; type: string; purpose?: string }[]
    expect(icons.map((icon) => `${icon.sizes} ${icon.purpose ?? 'any'}`)).toEqual(
        expect.arrayContaining(['192x192 any', '512x512 any', '512x512 maskable']),
    )

    const appleTouchIcon = await page
        .locator('link[rel="apple-touch-icon"]')
        .getAttribute('href')
    const pngIcons = [
        ...icons.filter((icon) => icon.type === 'image/png'),
        { src: appleTouchIcon ?? '', sizes: '180x180', type: 'image/png' },
    ]
    for (const icon of pngIcons) {
        const file = await request.get(icon.src)
        expect(file.status(), icon.src).toBe(200)
        expect(file.headers()['content-type'], icon.src).toBe('image/png')
        const { width, height } = pngSize(await file.body())
        expect(`${width}x${height}`, icon.src).toBe(icon.sizes)
    }
})

test('caches the app shell but never the API and works offline', async ({ page, context }) => {
    await page.goto('/library/songs')

    const scriptUrl = await page.evaluate(
        async () => (await navigator.serviceWorker.ready).active?.scriptURL ?? '',
    )
    expect(scriptUrl).toMatch(/\/sw\.js$/)
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null)

    const responses: { url: string; fromServiceWorker: boolean }[] = []
    page.on('response', (response) =>
        responses.push({ url: response.url(), fromServiceWorker: response.fromServiceWorker() }),
    )
    await page.reload()
    await expect(page.locator("[data-part='server-status']")).toHaveAttribute(
        'data-state',
        'healthy',
    )

    const health = responses.filter((response) => response.url.endsWith('/api/v1/health'))
    expect(health.length).toBeGreaterThan(0)
    expect(health.every((response) => !response.fromServiceWorker)).toBe(true)
    const script = responses.find((response) => /\/assets\/index-[^/]+\.js$/.test(response.url))
    expect(script?.fromServiceWorker).toBe(true)

    // Navigations to the API are never answered with the web app.
    const api = await page.goto('/api/v1/health')
    expect(api?.headers()['content-type']).toContain('application/json')
    expect(api?.fromServiceWorker()).toBe(false)
    expect((await api?.json()).status).toBe('Healthy')

    await context.setOffline(true)
    await page.goto('/library/songs')

    await expect(page.locator("[data-part='app']")).toBeVisible()
    await expect(page.locator("[data-part='server-status']")).toHaveAttribute(
        'data-state',
        'unreachable',
    )
    await context.setOffline(false)
})

test('is installable', async ({ page, context }) => {
    await page.goto('/library/songs')
    await page.evaluate(async () => {
        await navigator.serviceWorker.ready
    })

    const session = await context.newCDPSession(page)
    const { installabilityErrors } = (await session.send('Page.getInstallabilityErrors')) as {
        installabilityErrors: { errorId: string }[]
    }

    // Errors that come from the test browser itself (incognito context), not from the app.
    const environment = ['in-incognito']
    expect(
        installabilityErrors
            .map((error) => error.errorId)
            .filter((id) => !environment.includes(id)),
    ).toEqual([])
})
