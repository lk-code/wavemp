import { expect, test } from '@playwright/test'
import { adminToken, createUser, deleteUser, uniqueSuffix, type TestUser } from './helpers/api'

// Runs against the Docker image: profile names are normalized to Unicode NFC with ICU, so a name
// with a combining accent and the same name with a precomposed letter are one name.

const created: TestUser[] = []

test.afterEach(async ({ request }) => {
    for (const user of created.splice(0)) {
        await deleteUser(request, user.id)
    }
})

test('stores names with combining accents in the precomposed form', async ({ request }) => {
    const suffix = uniqueSuffix()

    const user = await createUser(request, `Renée Müller ${suffix}`)
    created.push(user)

    expect(user.name).toBe(`Renée Müller ${suffix}`)
})

test('treats combining and precomposed accents as the same name', async ({ request }) => {
    const suffix = uniqueSuffix()
    created.push(await createUser(request, `José ${suffix}`))

    const response = await request.post('/api/v1/users', {
        data: { name: `josé ${suffix}` },
        headers: { Authorization: `Bearer ${await adminToken(request)}` },
    })
    if (response.status() === 201) {
        // Only without normalization: clean up the duplicate as well.
        created.push(await response.json())
    }

    expect(response.status()).toBe(409)
    expect((await response.json()).code).toBe('user.name_taken')
})
