import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseAlert from '../BaseAlert.vue'
import BaseCheckbox from '../BaseCheckbox.vue'
import BaseProgress from '../BaseProgress.vue'
import BaseSelect from '../BaseSelect.vue'
import BaseTextField from '../BaseTextField.vue'

const options = [
    { value: 'en', label: 'English' },
    { value: 'de', label: 'Deutsch' },
]

describe('BaseTextField', () => {
    it('passes the input mode to the input', () => {
        const numeric = mount(BaseTextField, { props: { label: 'Year', inputmode: 'numeric' } })
        const plain = mount(BaseTextField, { props: { label: 'Name' } })

        expect(numeric.get('input').attributes('inputmode')).toBe('numeric')
        expect(numeric.get('input').attributes('type')).toBe('text')
        expect(plain.get('input').attributes('inputmode')).toBeUndefined()
    })

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

describe('BaseCheckbox', () => {
    it('renders a checkbox field and binds the checked state', async () => {
        const wrapper = mount(BaseCheckbox, {
            props: { label: 'Watch', modelValue: false, hint: 'Help' },
        })

        const field = wrapper.get('[data-part="field"]')
        expect(field.attributes('data-variant')).toBe('checkbox')
        expect(field.get('[data-part="label"]').text()).toBe('Watch')
        const input = field.get('input[type="checkbox"][data-part="checkbox"]')
        expect(input.attributes('aria-describedby')).toBe(
            field.get('[data-part="field-hint"]').attributes('id'),
        )

        await input.setValue(true)

        expect(wrapper.emitted('update:modelValue')).toEqual([[true]])
    })

    it('marks an error', () => {
        const wrapper = mount(BaseCheckbox, { props: { label: 'Watch', error: 'Wrong' } })

        expect(wrapper.get('[data-part="field"]').attributes('data-state')).toBe('invalid')
        expect(wrapper.get('input').attributes('aria-invalid')).toBe('true')
    })
})

describe('BaseProgress', () => {
    it('shows the share done as value and runtime variable', () => {
        const wrapper = mount(BaseProgress, {
            props: { label: 'Progress', value: 0.25, valueText: '1 of 4' },
        })

        const progress = wrapper.get('[data-part="progress"]')
        expect(progress.attributes('role')).toBe('progressbar')
        expect(progress.attributes('aria-label')).toBe('Progress')
        expect(progress.attributes('aria-valuenow')).toBe('25')
        expect(progress.attributes('aria-valuetext')).toBe('1 of 4')
        expect(progress.attributes('data-state')).toBeUndefined()
        expect(progress.attributes('style')).toContain('--wmp-progress-value: 0.25')
        expect(
            progress.find('[data-part="progress-track"] > [data-part="progress-fill"]').exists(),
        ).toBe(true)
    })

    it('is indeterminate without a value and keeps values between 0 and 1', () => {
        const indeterminate = mount(BaseProgress, { props: { label: 'Progress' } })
        const tooMuch = mount(BaseProgress, { props: { label: 'Progress', value: 3 } })

        expect(indeterminate.get('[data-part="progress"]').attributes('data-state')).toBe(
            'indeterminate',
        )
        expect(
            indeterminate.get('[data-part="progress"]').attributes('aria-valuenow'),
        ).toBeUndefined()
        expect(tooMuch.get('[data-part="progress"]').attributes('aria-valuenow')).toBe('100')
    })
})
