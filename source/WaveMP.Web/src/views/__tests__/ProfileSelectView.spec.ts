import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'

const api = vi.hoisted(() => ({
    listProfiles: vi.fn(),
    createSession: vi.fn(),
    getSettings: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                profiles: { get: () => api.listProfiles() },
                sessions: { post: (body: unknown) => api.createSession(body) },
                me: { settings: { get: () => api.getSettings() } },
            },
        },
    },
}))

const { default: ProfileSelectView } = await import('../ProfileSelectView.vue')
const { useSessionStore } = await import('@/stores/session')

const expiresAt = new Date(Date.now() + 24 * 60 * 60 * 1000)
const admin = {
    id: 'admin',
    name: 'Admin',
    initials: 'A',
    color: '#1f6fd6',
    role: 'Admin',
    isBuiltIn: true,
}
const anna = {
    id: 'anna',
    name: 'Anna',
    initials: 'A',
    color: '#2e7d32',
    role: 'User',
    isBuiltIn: false,
}

async function mountView(path = '/profiles') {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push(path)
    const wrapper = mount(ProfileSelectView, {
        global: { plugins: [router, createWaveMpI18n('en')] },
    })
    await flushPromises()
    return { wrapper, router }
}

describe('ProfileSelectView', () => {
    beforeEach(() => {
        localStorage.clear()
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
        api.getSettings.mockResolvedValue({ language: 'en' })
    })

    it('shows the profiles with the built-in admin as secondary', async () => {
        api.listProfiles.mockResolvedValue({ items: [anna, admin], total: 2 })

        const { wrapper } = await mountView()

        expect(wrapper.get('[data-part="profile-select"]').attributes('data-state')).toBe('ready')
        expect(wrapper.get('[data-part="profile-select-title"]').text()).toBe("Who's listening?")
        const tiles = wrapper.findAll('[data-part="profile-tile"]')
        expect(tiles.map((tile) => tile.text())).toEqual(['AAnna', 'AAdmin'])
        expect(tiles[1]?.attributes('data-state')).toBe('built-in secondary')
        expect(wrapper.find('[data-part="profile-select-hint"]').exists()).toBe(false)
    })

    it('shows a hint on the first start', async () => {
        api.listProfiles.mockResolvedValue({ items: [admin], total: 1 })

        const { wrapper } = await mountView()

        expect(wrapper.get('[data-part="profile-select-hint"]').text()).toBe(
            'Select the admin profile to create more profiles.',
        )
    })

    it('selects a profile and opens the requested page', async () => {
        api.listProfiles.mockResolvedValue({ items: [anna, admin], total: 2 })
        api.createSession.mockResolvedValue({
            token: 'token',
            session: { id: 'session', profile: anna, expiresAt },
        })
        const { wrapper, router } = await mountView('/profiles?redirect=/now-playing')

        await wrapper.findAll('[data-part="profile-tile"]')[0]?.trigger('click')
        await flushPromises()

        expect(api.createSession).toHaveBeenCalledWith({ profileId: 'anna' })
        expect(useSessionStore().profile?.id).toBe('anna')
        expect(router.currentRoute.value.fullPath).toBe('/now-playing')
    })

    it('reloads the profiles if the selected one no longer exists', async () => {
        api.listProfiles.mockResolvedValue({ items: [anna, admin], total: 2 })
        api.createSession.mockRejectedValue({ code: 'profile.not_found', responseStatusCode: 404 })
        const { wrapper } = await mountView()

        await wrapper.findAll('[data-part="profile-tile"]')[0]?.trigger('click')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"]').text()).toBe('This profile no longer exists.')
        expect(wrapper.get('[data-part="alert"]').attributes('role')).toBe('alert')
        expect(api.listProfiles).toHaveBeenCalledTimes(2)
    })

    it('reports other errors of the selection', async () => {
        api.listProfiles.mockResolvedValue({ items: [anna, admin], total: 2 })
        api.createSession.mockRejectedValue(new TypeError('Failed to fetch'))
        const { wrapper } = await mountView()

        await wrapper.findAll('[data-part="profile-tile"]')[0]?.trigger('click')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"]').text()).toBe(
            'The profile could not be selected. Please try again.',
        )
    })

    it('offers to try again if the profiles cannot be loaded', async () => {
        api.listProfiles.mockRejectedValueOnce(new TypeError('Failed to fetch'))
        api.listProfiles.mockResolvedValueOnce({ items: [admin], total: 1 })
        const { wrapper } = await mountView()

        expect(wrapper.get('[data-part="profile-select"]').attributes('data-state')).toBe('error')
        await wrapper.get('[data-part="empty-state"] button').trigger('click')
        await flushPromises()

        expect(wrapper.get('[data-part="profile-select"]').attributes('data-state')).toBe('ready')
        expect(wrapper.findAll('[data-part="profile-tile"]')).toHaveLength(1)
    })
})
