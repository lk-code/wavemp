import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import SettingsView from '../SettingsView.vue'
import { createWaveMpI18n } from '@/i18n'
import { useSessionStore } from '@/stores/session'

function mountView() {
    const session = useSessionStore()
    session.$patch({
        token: 'token',
        sessionId: 'session',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        profile: {
            id: 'anna',
            name: 'Anna Bell',
            initials: 'AB',
            color: '#2e7d32',
            role: 'User',
            isBuiltIn: false,
        },
        language: 'en',
    })
    const wrapper = mount(SettingsView, { global: { plugins: [createWaveMpI18n('en')] } })
    return { wrapper, session }
}

describe('SettingsView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('shows the profile and its language', () => {
        const { wrapper } = mountView()

        expect(wrapper.get('[data-part="view"]').attributes('data-view')).toBe('settings')
        expect(wrapper.get('[data-part="profile-summary"] [data-part="label"]').text()).toBe(
            'Anna Bell',
        )
        const select = wrapper.get<HTMLSelectElement>('[data-part="select"]')
        expect(select.element.value).toBe('en')
        expect(wrapper.findAll('option').map((option) => option.text())).toEqual([
            'English',
            'Deutsch',
        ])
        expect(wrapper.get('[data-part="field-hint"]').text()).toBe(
            'Applies to this profile on all devices.',
        )
    })

    it('saves a new language at once', async () => {
        const { wrapper, session } = mountView()
        const updateLanguage = vi.spyOn(session, 'updateLanguage').mockResolvedValue()

        await wrapper.get('[data-part="select"]').setValue('de')
        await flushPromises()

        expect(updateLanguage).toHaveBeenCalledWith('de')
        expect(wrapper.find('[data-part="alert"]').exists()).toBe(false)
    })

    it('reports an error and keeps the saved language', async () => {
        const { wrapper, session } = mountView()
        vi.spyOn(session, 'updateLanguage').mockRejectedValue(new TypeError('Failed to fetch'))

        await wrapper.get('[data-part="select"]').setValue('de')
        await flushPromises()

        expect(wrapper.get('[data-part="alert"]').text()).toBe('The setting could not be saved.')
        expect(wrapper.get<HTMLSelectElement>('[data-part="select"]').element.value).toBe('en')
    })
})
