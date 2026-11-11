import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import VueI18nPlugin from '@intlify/unplugin-vue-i18n/vite'

// Address of WaveMP.Service during development (see Properties/launchSettings.json).
const serviceUrl = process.env.WAVEMP_SERVICE_URL ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
    plugins: [
        vue(),
        vueDevTools(),
        // Precompiles the messages in src/locales, so the app uses the runtime-only build of
        // vue-i18n (no message compiler and no eval at runtime).
        VueI18nPlugin({
            include: fileURLToPath(new URL('./src/locales/**', import.meta.url)),
            strictMessage: true,
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
