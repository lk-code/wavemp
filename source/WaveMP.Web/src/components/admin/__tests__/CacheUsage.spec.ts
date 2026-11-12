import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import CacheClearDialog from '../CacheClearDialog.vue'
import CacheUsage from '../CacheUsage.vue'
import type { CacheUsage as CacheUsageData } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'

const usage: CacheUsageData = {
    isAvailable: true,
    sizeBytes: 500_000_000,
    entryCount: 1234,
    maxSizeBytes: 2_000_000_000,
    covers: { sizeBytes: 500_000_000, entryCount: 1234 },
    transcodes: { sizeBytes: 0, entryCount: 0 },
}

function mountUsage(value: CacheUsageData, clearing = false) {
    return mount(CacheUsage, {
        props: { usage: value, clearing },
        global: { plugins: [createWaveMpI18n('en')] },
    })
}

describe('CacheUsage', () => {
    it('shows the used space against the limit and what the cache holds', () => {
        const wrapper = mountUsage(usage)

        const meter = wrapper.get('[data-part="meter"]')
        expect(meter.attributes('aria-valuenow')).toBe('25')
        expect(meter.attributes('aria-valuetext')).toBe('500 MB of 2 GB used')
        expect(wrapper.get('[data-part="cache-usage"] > [data-part="label"]').text()).toBe(
            '500 MB of 2 GB used',
        )
        const items = wrapper.findAll('[data-part="stat-item"]')
        expect(items.map((item) => item.attributes('data-item'))).toEqual([
            'covers',
            'transcodes',
            'entries',
        ])
        expect(items.map((item) => item.get('[data-part="stat-value"]').text())).toEqual([
            '500 MB',
            '0 byte',
            '1,234',
        ])
        expect(wrapper.text()).toContain('Holds scaled covers and transcoded media.')
    })

    it('asks to clear the cache unless it is empty or being cleared', async () => {
        const wrapper = mountUsage(usage)

        await wrapper.get('button').trigger('click')

        expect(wrapper.get('button').text()).toBe('Clear cache')
        expect(wrapper.emitted('clear')).toHaveLength(1)
        expect(
            mountUsage({ ...usage, entryCount: 0 })
                .get('button')
                .attributes('disabled'),
        ).toBeDefined()
        expect(mountUsage(usage, true).get('button').attributes('disabled')).toBeDefined()
    })
})

describe('CacheClearDialog', () => {
    it('asks before clearing and confirms', async () => {
        const wrapper = mount(CacheClearDialog, {
            props: { open: false },
            global: { plugins: [createWaveMpI18n('en')] },
            attachTo: document.body,
        })
        await wrapper.setProps({ open: true })

        expect(wrapper.get('[data-part="dialog-title"]').text()).toBe('Clear cache')
        expect(wrapper.text()).toContain('Remove all files of the cache?')
        const confirm = wrapper
            .findAll('[data-part="dialog-footer"] button')
            .find((button) => button.text() === 'Clear')
        await confirm!.trigger('click')

        expect(wrapper.emitted('confirm')).toHaveLength(1)
        wrapper.unmount()
    })
})
