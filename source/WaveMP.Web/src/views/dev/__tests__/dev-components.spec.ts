import { describe, expect, it } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import DevComponentsView from '../DevComponentsView.vue'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes, devRoutes } from '@/router/routes'
import { devPartPrefix, parts } from '@/themes/parts'

// Parts that only the app shell renders around the page content. The overview shows them in the
// previews of the real app (iframes), not as single components.
const shownInPreviewsOnly = ['app', 'content', 'content-title']

async function mountOverview() {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes(devRoutes) })
    await router.push('/dev/components')
    const wrapper = mount(DevComponentsView, {
        global: { plugins: [router, createPinia(), createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return wrapper
}

describe('component overview', () => {
    it('is a route of the development build', () => {
        expect(import.meta.env.DEV).toBe(true)
        expect(devRoutes.map((route) => route.path)).toEqual(['/dev/components'])
        expect(devRoutes[0]?.meta?.bare).toBe(true)
    })

    it('renders every part of the theme API', async () => {
        const wrapper = await mountOverview()

        const rendered = new Set(
            [...wrapper.element.querySelectorAll('[data-part]')].map(
                (element) => element.getAttribute('data-part') ?? '',
            ),
        )
        const missing = parts.filter(
            (part) => !rendered.has(part) && !shownInPreviewsOnly.includes(part),
        )
        expect(missing).toEqual([])

        // Everything else on the page is part of the overview itself.
        const unknown = [...rendered].filter(
            (part) =>
                !(parts as readonly string[]).includes(part) && !part.startsWith(devPartPrefix),
        )
        expect(unknown).toEqual([])

        wrapper.unmount()
    })

    it('shows both layouts of the real app', async () => {
        const wrapper = await mountOverview()

        const frames = wrapper.findAll('[data-part="dev-frame"]')
        expect(frames.map((frame) => frame.attributes('data-device'))).toEqual([
            'mobile',
            'desktop',
        ])
        expect(frames.every((frame) => frame.attributes('src') === '/library/songs')).toBe(true)

        wrapper.unmount()
    })

    it('shows the transport bar in all states', async () => {
        const wrapper = await mountOverview()

        const states = wrapper
            .findAll('[data-part="transport"]')
            .map((transport) => transport.attributes('data-state'))
        expect(states).toEqual(
            expect.arrayContaining(['idle', 'playing', 'paused', 'playing expanded']),
        )
        expect(wrapper.find('[data-part="btn-repeat"][data-state="one"]').exists()).toBe(true)
        expect(wrapper.find('[data-part="btn-repeat"][data-state="all"]').exists()).toBe(true)
        expect(wrapper.find('[data-part="btn-shuffle"][data-state="on"]').exists()).toBe(true)
        expect(wrapper.find('[data-part="btn-mute"][data-state="muted"]').exists()).toBe(true)

        wrapper.unmount()
    })

    it('switches the language', async () => {
        const wrapper = await mountOverview()

        const german = wrapper
            .findAll('[data-part="dev-language"] button')
            .find((button) => button.text() === 'Deutsch')
        await german?.trigger('click')

        expect(wrapper.get('[data-part="dev-title"]').text()).toBe('Komponentenübersicht')
        expect(document.documentElement.lang).toBe('de')

        wrapper.unmount()
    })
})
