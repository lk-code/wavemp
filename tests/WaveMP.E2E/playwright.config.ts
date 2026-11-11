import { defineConfig, devices } from '@playwright/test'
import { adminStorageState } from './tests/helpers/storage'

/**
 * Runs against a running WaveMP instance, e.g. started with
 * `docker compose -f docker-compose.dev.yml up --build`.
 * The project "setup" selects the built-in admin profile once; the other projects start with its
 * session (see tests/helpers/storage.ts).
 */
export default defineConfig({
    testDir: './tests',
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
    use: {
        baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8080',
        trace: 'on-first-retry',
    },
    projects: [
        { name: 'setup', testMatch: /.*\.setup\.ts/, use: { ...devices['Desktop Chrome'] } },
        {
            name: 'chromium',
            use: { ...devices['Desktop Chrome'], storageState: adminStorageState },
            dependencies: ['setup'],
        },
        {
            name: 'mobile',
            use: { ...devices['Pixel 7'], storageState: adminStorageState },
            dependencies: ['setup'],
        },
    ],
})
