import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ProfileList from '../ProfileList.vue'
import ProfileTile from '../ProfileTile.vue'
import { createWaveMpI18n } from '@/i18n'

const anna = { id: 'anna', name: 'Anna Bell', initials: 'AB', color: '#2e7d32', isBuiltIn: false }
const admin = { id: 'admin', name: 'Admin', initials: 'A', color: '#1f6fd6', isBuiltIn: true }

const global = { plugins: [createWaveMpI18n('en')] }

describe('ProfileTile', () => {
    it('shows initials on the profile color and the name', () => {
        const wrapper = mount(ProfileTile, { props: { profile: anna }, global })

        const avatar = wrapper.get('[data-part="avatar"]')
        expect(avatar.attributes('aria-hidden')).toBe('true')
        expect(avatar.attributes('style')).toContain('--wmp-profile-color: #2e7d32')
        expect(wrapper.get('[data-part="avatar-initials"]').text()).toBe('AB')
        expect(wrapper.get('[data-part="label"]').text()).toBe('Anna Bell')
        expect(wrapper.get('button').attributes('title')).toBeUndefined()
    })

    it('marks the built-in admin and the current profile', () => {
        const wrapper = mount(ProfileTile, {
            props: { profile: admin, state: 'built-in secondary current' },
            global,
        })

        const button = wrapper.get('[data-part="profile-tile"]')
        expect(button.attributes('data-state')).toBe('built-in secondary current')
        expect(button.attributes('aria-current')).toBe('true')
        expect(button.attributes('title')).toBe('Built-in admin profile')
    })

    it('emits select on click', async () => {
        const wrapper = mount(ProfileTile, { props: { profile: anna }, global })

        await wrapper.get('button').trigger('click')

        expect(wrapper.emitted('select')).toHaveLength(1)
    })
})

describe('ProfileList', () => {
    it('lists the profiles with their states', async () => {
        const wrapper = mount(ProfileList, {
            props: { profiles: [anna, admin], currentId: 'anna' },
            global,
        })

        const list = wrapper.get('[data-part="profile-list"]')
        expect(list.attributes('aria-label')).toBe('Profiles')
        const tiles = wrapper.findAll('[data-part="profile-item"] > [data-part="profile-tile"]')
        expect(tiles.map((tile) => tile.attributes('data-state'))).toEqual([
            'current',
            'built-in secondary',
        ])

        await tiles[1]?.trigger('click')

        expect(wrapper.emitted('select')).toEqual([[admin]])
    })
})
