import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseButton from '../BaseButton.vue'
import BaseRoundButton from '../BaseRoundButton.vue'

describe('BaseButton', () => {
    it('renders the label in its own part and the default variant', () => {
        const wrapper = mount(BaseButton, { props: { label: 'Save' } })
        const button = wrapper.get('button')

        expect(button.attributes('type')).toBe('button')
        expect(button.attributes('data-part')).toBe('button')
        expect(button.attributes('data-variant')).toBe('default')
        expect(wrapper.get('[data-part="label"]').text()).toBe('Save')
        expect(wrapper.find('[data-part="icon"]').exists()).toBe(false)
        expect(button.attributes('title')).toBeUndefined()
        expect(button.attributes('aria-pressed')).toBeUndefined()
    })

    it('renders the icon in its own part before the label', () => {
        const wrapper = mount(BaseButton, {
            props: { label: 'Play', icon: 'play', part: 'btn-play', variant: 'round' },
        })

        const children = wrapper.get('button').element.children
        expect(children[0]?.getAttribute('data-part')).toBe('icon')
        expect(children[0]?.getAttribute('data-icon')).toBe('play')
        expect(children[0]?.getAttribute('aria-hidden')).toBe('true')
        expect(children[0]?.firstElementChild?.getAttribute('data-part')).toBe('icon-glyph')
        expect(children[1]?.getAttribute('data-part')).toBe('label')
        expect(wrapper.get('button').attributes('data-part')).toBe('btn-play')
    })

    it('shows the label as tooltip for icon and round buttons', () => {
        expect(
            mount(BaseButton, { props: { label: 'Mute', variant: 'icon' } })
                .get('button')
                .attributes('title'),
        ).toBe('Mute')
        expect(
            mount(BaseButton, { props: { label: 'Mute', variant: 'icon', tooltip: false } })
                .get('button')
                .attributes('title'),
        ).toBeUndefined()
    })

    it('reflects toggle state, data-state and disabled', async () => {
        const wrapper = mount(BaseButton, {
            props: { label: 'Shuffle', pressed: false, state: 'off', disabled: true },
        })
        const button = wrapper.get('button')

        expect(button.attributes('aria-pressed')).toBe('false')
        expect(button.attributes('data-state')).toBe('off')
        expect(button.attributes('disabled')).toBeDefined()

        await wrapper.setProps({ pressed: true, state: 'on', disabled: false })

        expect(button.attributes('aria-pressed')).toBe('true')
        expect(button.attributes('data-state')).toBe('on')
        expect(button.attributes('disabled')).toBeUndefined()
    })

    it('adds the dropdown icon after the label', () => {
        const wrapper = mount(BaseButton, {
            props: { label: 'Sort', icon: 'sort', dropdown: true },
        })

        const icons = wrapper.findAll('[data-part="icon"]')
        expect(icons.map((icon) => icon.attributes('data-icon'))).toEqual(['sort', 'dropdown'])
    })

    it('emits clicks', async () => {
        const wrapper = mount(BaseButton, { props: { label: 'Save' } })

        await wrapper.get('button').trigger('click')

        expect(wrapper.emitted('click')).toHaveLength(1)
    })
})

describe('BaseRoundButton', () => {
    it('renders a round button with icon and tooltip', () => {
        const wrapper = mount(BaseRoundButton, {
            props: { label: 'Back', icon: 'back', part: 'btn-back' },
        })
        const button = wrapper.get('button')

        expect(button.attributes('data-variant')).toBe('round')
        expect(button.attributes('data-part')).toBe('btn-back')
        expect(button.attributes('title')).toBe('Back')
        expect(wrapper.get('[data-part="icon"]').attributes('data-icon')).toBe('back')
    })
})
