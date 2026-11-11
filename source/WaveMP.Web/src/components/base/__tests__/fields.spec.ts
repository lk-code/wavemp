import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseAlert from '../BaseAlert.vue'
import BaseSelect from '../BaseSelect.vue'
import BaseTextField from '../BaseTextField.vue'

const options = [
    { value: 'en', label: 'English' },
    { value: 'de', label: 'Deutsch' },
]

describe('BaseTextField', () => {
    it('renders label and input in a field', () => {
        const wrapper = mount(BaseTextField, {
            props: { label: 'Name', required: true, maxlength: 50, autocomplete: 'off' },
        })

        const field = wrapper.get('[data-part="field"]')
        expect(field.attributes('data-state')).toBeUndefined()
        expect(field.get('[data-part="label"]').text()).toBe('Name')
        const input = field.get('input[data-part="input"]')
        expect(input.attributes('required')).toBeDefined()
        expect(input.attributes('maxlength')).toBe('50')
        expect(input.attributes('autocomplete')).toBe('off')
        expect(input.attributes('aria-invalid')).toBeUndefined()
        expect(input.attributes('aria-describedby')).toBeUndefined()
    })

    it('describes the input with hint and error', () => {
        const wrapper = mount(BaseTextField, {
            props: { label: 'Name', hint: 'Your name', error: 'Taken' },
        })

        const field = wrapper.get('[data-part="field"]')
        const hint = field.get('[data-part="field-hint"]')
        const error = field.get('[data-part="field-error"]')
        expect(field.attributes('data-state')).toBe('invalid')
        expect(hint.text()).toBe('Your name')
        expect(error.text()).toBe('Taken')
        const input = field.get('input')
        expect(input.attributes('aria-invalid')).toBe('true')
        expect(input.attributes('aria-describedby')).toBe(
            `${error.attributes('id')} ${hint.attributes('id')}`,
        )
    })

    it('updates the model', async () => {
        const wrapper = mount(BaseTextField, { props: { label: 'Name', modelValue: '' } })

        await wrapper.get('input').setValue('Anna')

        expect(wrapper.emitted('update:modelValue')).toEqual([['Anna']])
    })
})

describe('BaseSelect', () => {
    it('renders the options and selects the model value', () => {
        const wrapper = mount(BaseSelect, {
            props: { label: 'Language', options, modelValue: 'de' },
        })

        const select = wrapper.get<HTMLSelectElement>('select[data-part="select"]')
        expect(wrapper.get('[data-part="label"]').text()).toBe('Language')
        expect(wrapper.findAll('option').map((option) => option.text())).toEqual([
            'English',
            'Deutsch',
        ])
        expect(select.element.value).toBe('de')
    })

    it('updates the model and shows hint and error', async () => {
        const wrapper = mount(BaseSelect, {
            props: { label: 'Language', options, modelValue: 'en', hint: 'Hint', error: 'Error' },
        })

        await wrapper.get('select').setValue('de')

        expect(wrapper.emitted('update:modelValue')).toEqual([['de']])
        expect(wrapper.get('[data-part="field"]').attributes('data-state')).toBe('invalid')
        expect(wrapper.get('select').attributes('aria-invalid')).toBe('true')
        expect(wrapper.get('[data-part="field-hint"]').text()).toBe('Hint')
        expect(wrapper.get('[data-part="field-error"]').text()).toBe('Error')
    })
})

describe('BaseAlert', () => {
    it('announces errors at once and other messages politely', () => {
        const error = mount(BaseAlert, { props: { message: 'Failed', tone: 'error' } })
        const info = mount(BaseAlert, { props: { message: 'Saved' } })

        expect(error.get('[data-part="alert"]').attributes('role')).toBe('alert')
        expect(error.get('[data-part="alert"]').attributes('data-state')).toBe('error')
        expect(error.get('[data-part="label"]').text()).toBe('Failed')
        expect(info.get('[data-part="alert"]').attributes('role')).toBe('status')
        expect(info.get('[data-part="alert"]').attributes('data-state')).toBe('info')
    })
})
