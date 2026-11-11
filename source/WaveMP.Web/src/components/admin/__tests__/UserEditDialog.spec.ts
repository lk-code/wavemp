import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import UserEditDialog from '../UserEditDialog.vue'
import type { UserListItem } from '../UserList.vue'
import { createWaveMpI18n } from '@/i18n'
import { useUsersStore } from '@/stores/users'

const anna: UserListItem = {
    id: 'anna',
    name: 'Anna Bell',
    initials: 'AB',
    color: '#2e7d32',
    isAdmin: false,
    isBuiltIn: false,
    createdAt: new Date(),
}

async function mountDialog(user: UserListItem | null) {
    const wrapper = mount(UserEditDialog, {
        props: { user, open: false },
        global: { plugins: [createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await wrapper.setProps({ open: true })
    return wrapper
}

async function submit(wrapper: Awaited<ReturnType<typeof mountDialog>>) {
    await wrapper.get('form').trigger('submit')
    await flushPromises()
}

describe('UserEditDialog', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('creates a profile with the entered values and leaves empty initials to the server', async () => {
        const users = useUsersStore()
        const create = vi.spyOn(users, 'create').mockResolvedValue({ id: 'new', name: 'Carla' })
        const wrapper = await mountDialog(null)

        expect(wrapper.get('[data-part="dialog-title"]').text()).toBe('Add profile')
        await wrapper.findAll('input')[0]?.setValue('  Carla  ')
        await wrapper.findAll('[data-part="color-swatch"]')[2]?.trigger('click')
        await submit(wrapper)

        expect(create).toHaveBeenCalledWith({
            name: 'Carla',
            initials: undefined,
            color: '#2e7d32',
            role: 'User',
        })
        expect(wrapper.emitted('saved')).toEqual([[{ id: 'new', name: 'Carla' }]])
        expect(wrapper.emitted('update:open')?.at(-1)).toEqual([false])
        wrapper.unmount()
    })

    it('sends only the changed values of an existing profile', async () => {
        const users = useUsersStore()
        const update = vi.spyOn(users, 'update').mockResolvedValue({ id: 'anna' })
        const wrapper = await mountDialog(anna)

        expect(wrapper.get('[data-part="dialog-title"]').text()).toBe('Edit profile')
        await wrapper.findAll('input')[1]?.setValue('XY')
        await wrapper.get('select').setValue('Admin')
        await submit(wrapper)

        expect(update).toHaveBeenCalledWith('anna', { initials: 'XY', role: 'Admin' })
        wrapper.unmount()
    })

    it('closes without a request if nothing changed', async () => {
        const users = useUsersStore()
        const update = vi.spyOn(users, 'update')
        const wrapper = await mountDialog(anna)

        await submit(wrapper)

        expect(update).not.toHaveBeenCalled()
        expect(wrapper.emitted('update:open')?.at(-1)).toEqual([false])
        wrapper.unmount()
    })

    it('shows a taken name at the name field', async () => {
        const users = useUsersStore()
        vi.spyOn(users, 'update').mockRejectedValue({
            code: 'user.name_taken',
            responseStatusCode: 409,
        })
        const wrapper = await mountDialog(anna)

        await wrapper.findAll('input')[0]?.setValue('Admin')
        await submit(wrapper)

        const nameField = wrapper.findAll('[data-part="field"]')[0]
        expect(nameField?.attributes('data-state')).toBe('invalid')
        expect(nameField?.get('[data-part="field-error"]').text()).toBe(
            'A profile with this name already exists.',
        )
        expect(wrapper.emitted('saved')).toBeUndefined()
        wrapper.unmount()
    })

    it('needs a name', async () => {
        const users = useUsersStore()
        const create = vi.spyOn(users, 'create')
        const wrapper = await mountDialog(null)

        await submit(wrapper)

        expect(create).not.toHaveBeenCalled()
        expect(wrapper.get('[data-part="field-error"]').text()).toBe('Please enter a name.')
        wrapper.unmount()
    })

    it('reports other errors', async () => {
        const users = useUsersStore()
        vi.spyOn(users, 'create').mockRejectedValue({ responseStatusCode: 400 })
        const wrapper = await mountDialog(null)

        await wrapper.findAll('input')[0]?.setValue('Carla')
        await submit(wrapper)

        expect(wrapper.get('[data-part="alert"]').text()).toBe('Please check your input.')
        wrapper.unmount()
    })

    it('keeps the role of the built-in admin and offers its own color', async () => {
        const wrapper = await mountDialog({
            ...anna,
            id: 'admin',
            isAdmin: true,
            isBuiltIn: true,
            color: '#123456',
        })

        expect(wrapper.get('select').attributes('disabled')).toBeDefined()
        expect(wrapper.text()).toContain('The built-in admin profile always stays an admin.')
        const swatches = wrapper.findAll('[data-part="color-swatch"]')
        expect(swatches).toHaveLength(9)
        expect(swatches.at(-1)?.attributes('aria-checked')).toBe('true')
        expect(swatches.at(-1)?.attributes('title')).toBe('Current color')
        wrapper.unmount()
    })
})
