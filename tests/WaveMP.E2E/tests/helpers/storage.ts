import { fileURLToPath } from 'node:url'

/**
 * Browser state with a session of the built-in admin profile, written once by auth.setup.ts and
 * used by most tests. Tests that sign out or switch the profile must not use it: ending this
 * session would end it for all tests.
 */
export const adminStorageState = fileURLToPath(
    new URL('../../playwright/.auth/admin.json', import.meta.url),
)

/** Browser state without a selected profile. */
export const emptyStorageState = { cookies: [], origins: [] }
