import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import AdminUsersView from '../AdminUsersView.vue'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'
import { useSessionStore } from '@/stores/session'
import { useUsersStore } from '@/stores/users'

const admin = {
    id: 'admin',
    name: 'Admin',
    initials: 'A',
    color: '#1f6fd6',
    role: 'Admin' as const,
    isBuiltIn: true,
    createdAt: new Date(Date.UTC(2026, 10, 1)),
}
const anna = {
    id: 'anna',
    name: 'Anna',
    initials: 'A',
    color: '#2e7d32',
    role: 'User' as const,
    isBuiltIn: false,
    createdAt: new Date(Date.UTC(2026, 10, 2)),
}

async function mountView(profileId = 'admin') {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push('/admin/users')
    const session = useSessionStore()
    session.$patch({
        token: 'token',
        sessionId: 'session',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        profile: { ...admin, id: profileId, role: 'Admin' },
        language: 'en',
    })
    const users = useUsersStore()
    const load = vi.spyOn(users, 'load').mockImplementation(async () => {
        users.users = [anna, admin]
        users.status = 'ready'
    })
    const wrapper = mount(AdminUsersView, {
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return { wrapper, router, users, session, load }
}

describe('AdminUsersView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('lists the profiles with role, built-in state and actions', async () => {
        const { wrapper } = await mountView()

        expect(wrapper.get('[data-part="view"]').attributes('data-view')).toBe('admin-users')
        const items = wrapper.findAll('[data-part="user-item"]')
        expect(items.map((item) => item.get('[data-part="user-name"]').text())).toEqual([
            'Anna',
            'Admin',
        ])
        expect(items[1]?.attributes('data-state')).toBe('built-in')
        expect(items[1]?.findAll('[data-part="badge"]').map((badge) => badge.text())).toEqual([
            'Admin',
            'Built-in',
        ])
        expect(items[0]?.get('[data-part="user-meta"]').text()).toContain('Created')
        const deleteAdmin = items[1]?.findAll('[data-part="user-actions"] button')[1]
        expect(deleteAdmin?.attributes('disabled')).toBeDefined()
        expect(deleteAdmin?.attributes('title')).toBe(
            'The built-in admin profile cannot be deleted.',
        )
        wrapper.unmount()
    })

    it('deletes a profile after the confirmation and reloads the list', async () => {
        const { wrapper, users, load } = await mountView()
        const remove = vi.spyOn(users, 'remove').mockResolvedValue()

        await wrapper.findAll('[data-part="user-item"]')[0]?.findAll('button')[1]?.trigger('click')
        await flushPromises()
        expect(document.body.textContent).toContain(
            'Delete the profile Anna? This cannot be undone.',
        )
        const confirm = wrapper
            .findAll('[data-part="dialog-footer"] button')
            .find((button) => button.text() === 'Delete')
        await confirm?.trigger('click')
        await flushPromises()

        expect(remove).toHaveBeenCalledWith('anna')
        expect(load).toHaveBeenCalledTimes(2)
        wrapper.unmount()
    })

    it('reports a failed deletion', async () => {
        const { wrapper, users } = await mountView()
        vi.spyOn(users, 'remove').mockRejectedValue(new TypeError('Failed to fetch'))

        await wrapper.findAll('[data-part="user-item"]')[0]?.findAll('button')[1]?.trigger('click')
        const confirm = wrapper
            .findAll('[data-part="dialog-footer"] button')
            .find((button) => button.text() === 'Delete')
        await confirm?.trigger('click')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"]').text()).toBe('The profile could not be deleted.')
        wrapper.unmount()
    })

    it('leads to the profile selection after deleting the own profile', async () => {
        const { wrapper, users, session, router } = await mountView('anna')
        vi.spyOn(users, 'remove').mockResolvedValue()

        await wrapper.findAll('[data-part="user-item"]')[0]?.findAll('button')[1]?.trigger('click')
        const confirm = wrapper
            .findAll('[data-part="dialog-footer"] button')
            .find((button) => button.text() === 'Delete')
        await confirm?.trigger('click')
        await flushPromises()

        expect(router.currentRoute.value.name).toBe('profiles')
        expect(session.isAuthenticated).toBe(false)
        wrapper.unmount()
    })
})
