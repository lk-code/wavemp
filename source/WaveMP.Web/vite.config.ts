import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import VueI18nPlugin from '@intlify/unplugin-vue-i18n/vite'
import { VitePWA } from 'vite-plugin-pwa'

// Address of WaveMP.Service during development (see Properties/launchSettings.json).
const serviceUrl = process.env.WAVEMP_SERVICE_URL ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
    // Graphics, icons and themes from /assets/web are served unchanged at the root (e.g.
    // /themes/midnight/theme.css), in development and in the build.
    publicDir: fileURLToPath(new URL('../../assets/web', import.meta.url)),
    plugins: [
        vue(),
        vueDevTools(),
        // Precompiles the messages in src/locales, so the app uses the runtime-only build of
        // vue-i18n (no message compiler and no eval at runtime).
        VueI18nPlugin({
            include: fileURLToPath(new URL('./src/locales/**', import.meta.url)),
            strictMessage: true,
        }),
        // Installable web app: manifest and a service worker that precaches the app shell (HTML,
        // scripts, styles, themes, icons). API responses and media are never cached.
        VitePWA({
            // New versions activate on the next load without asking and without a forced reload.
            registerType: 'autoUpdate',
            // Registration through /registerSW.js, no inline script.
            injectRegister: 'script-defer',
            manifest: {
                id: '/',
                name: 'WaveMP',
                short_name: 'WaveMP',
                description:
                    'A nostalgic media player for your self-hosted music and music videos.',
                lang: 'en',
                dir: 'ltr',
                start_url: '/',
                scope: '/',
                display: 'standalone',
                background_color: '#0b0d10',
                theme_color: '#000000',
                icons: [
                    { src: '/brand/pwa-192x192.png', sizes: '192x192', type: 'image/png' },
                    { src: '/brand/pwa-512x512.png', sizes: '512x512', type: 'image/png' },
                    {
                        src: '/brand/pwa-maskable-512x512.png',
                        sizes: '512x512',
                        type: 'image/png',
                        purpose: 'maskable',
                    },
                    { src: '/brand/logo.svg', sizes: 'any', type: 'image/svg+xml' },
                ],
            },
            workbox: {
                globPatterns: ['**/*.{js,css,html,svg,png,webp,woff2}'],
                navigateFallback: '/index.html',
                // The API, the OpenAPI document and paths that look like files (as on the server)
                // always come from the network.
                navigateFallbackDenylist: [
                    /^\/api(?:\/|$)/,
                    /^\/openapi(?:\/|$)/,
                    /\/[^/?]+\.[^/?]+$/,
                ],
                cleanupOutdatedCaches: true,
                // A new service worker takes over at once (the registration script sends no
                // message for that); the app shell of the new version is used on the next load.
                skipWaiting: true,
                clientsClaim: true,
                // No runtime caching: API responses and media are never cached.
                runtimeCaching: [],
            },
            devOptions: { enabled: false },
        }),
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    server: {
        proxy: {
            '/api': serviceUrl,
            '/openapi': serviceUrl,
        },
    },
})
