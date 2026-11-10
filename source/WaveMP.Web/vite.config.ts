import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// Address of WaveMP.Service during development (see Properties/launchSettings.json).
const serviceUrl = process.env.WAVEMP_SERVICE_URL ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
    plugins: [vue(), vueDevTools()],
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
