import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import ProfileMenu, { type ProfileMenuProfile } from '../ProfileMenu.vue'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'
import { useSessionStore } from '@/stores/session'

const profile: ProfileMenuProfile = { name: 'Anna Bell', initials: 'AB', color: '#2e7d32' }

async function mountMenu(menuProfile: ProfileMenuProfile = profile) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push('/library/songs')
    const wrapper = mount(ProfileMenu, {
        props: { profile: menuProfile },
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    return { wrapper, router }
}

describe('ProfileMenu', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows the avatar of the profile and names it', async () => {
        const { wrapper } = await mountMenu()

        const button = wrapper.get('[data-part="btn-profile"]')
        expect(button.attributes('aria-haspopup')).toBe('menu')
        expect(button.attributes('aria-expanded')).toBe('false')
        expect(button.attributes('title')).toBe('Profile: Anna Bell')
        expect(button.get('[data-part="avatar-initials"]').text()).toBe('AB')
        expect(button.get('[data-part="label"]').text()).toBe('Profile: Anna Bell')
        wrapper.unmount()
    })

    it('opens the menu with settings and the profile switch', async () => {
        const { wrapper } = await mountMenu()

        await wrapper.get('[data-part="btn-profile"]').trigger('click')
        await flushPromises()

        expect(wrapper.get('[data-part="btn-profile"]').attributes('aria-expanded')).toBe('true')
        const items = wrapper.findAll('[data-part="menu-item"]').map((item) => item.text())
        expect(items).toEqual(['Settings', 'Switch profile'])
        wrapper.unmount()
    })

    it('offers the profile administration to admins', async () => {
        const { wrapper, router } = await mountMenu({ ...profile, role: 'Admin' })

        const items = wrapper.findAll('[data-part="menu-item"]').map((item) => item.text())
        expect(items).toEqual(['Settings', 'Manage profiles', 'Switch profile'])

        await wrapper.get('[data-item="manage-profiles"]').trigger('click')
        await flushPromises()

        expect(router.currentRoute.value.name).toBe('admin-users')
        wrapper.unmount()
    })

    it('opens the settings', async () => {
        const { wrapper, router } = await mountMenu()

        await wrapper.get('[data-item="settings"]').trigger('click')
        await flushPromises()

        expect(router.currentRoute.value.name).toBe('settings')
        wrapper.unmount()
    })

    it('switches the profile: opens the selection and ends the session', async () => {
        const session = useSessionStore()
        const signOut = vi.spyOn(session, 'signOut').mockResolvedValue()
        const { wrapper, router } = await mountMenu()

        await wrapper.get('[data-item="switch-profile"]').trigger('click')
        await flushPromises()

        expect(router.currentRoute.value.fullPath).toBe('/profiles')
        expect(signOut).toHaveBeenCalledOnce()
        wrapper.unmount()
    })
})
