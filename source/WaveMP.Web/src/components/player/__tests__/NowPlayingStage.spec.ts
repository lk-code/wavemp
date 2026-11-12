import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import type { Track } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import NowPlayingStage from '../NowPlayingStage.vue'

vi.mock('@/library/api', () => ({ fetchAlbumCover: vi.fn(), fetchTrackCover: vi.fn() }))

function track(hasVideo: boolean): Track {
    return {
        id: 'a',
        title: 'A',
        hasAudio: true,
        hasVideo,
        hasCover: false,
        artist: { id: 'band', name: 'Band' },
    }
}

function mountStage(props: { track?: Track; videoMode?: boolean }) {
    return mount(NowPlayingStage, {
        props,
        global: { plugins: [createPinia(), createWaveMpI18n('en')] },
        attachTo: document.body,
    })
}

describe('NowPlayingStage', () => {
    it('shows the cover and no video button for songs without a video', () => {
        const wrapper = mountStage({ track: track(false), videoMode: true })

        expect(wrapper.get('[data-part="stage"]').attributes('data-state')).toBe('cover')
        expect(wrapper.find('[data-part="cover"]').exists()).toBe(true)
        expect(wrapper.find('[data-part="stage-actions"]').exists()).toBe(false)
        wrapper.unmount()
    })

    it('shows the cover of a song with a video while the video mode is off', async () => {
        const wrapper = mountStage({ track: track(true), videoMode: false })

        const button = wrapper.get('[data-part="btn-video"]')
        expect(button.attributes('aria-pressed')).toBe('false')
        expect(wrapper.find('[data-part="video-stage"]').exists()).toBe(false)

        await button.trigger('click')
        expect(wrapper.emitted('toggleVideo')).toHaveLength(1)
        wrapper.unmount()
    })

    it('hands the video element over while the video is shown', async () => {
        const wrapper = mountStage({ track: track(true), videoMode: true })

        expect(wrapper.get('[data-part="stage"]').attributes('data-state')).toBe('video')
        expect(wrapper.get('[data-part="btn-video"]').attributes('aria-pressed')).toBe('true')
        const element = wrapper.get('[data-part="video"]').element
        expect(wrapper.emitted('attachVideo')).toEqual([[element]])
        // jsdom has no full screen.
        expect(wrapper.get('[data-part="btn-fullscreen"]').attributes('disabled')).toBeDefined()

        await wrapper.setProps({ videoMode: false })

        expect(wrapper.emitted('detachVideo')).toEqual([[element]])
        expect(wrapper.find('[data-part="video-stage"]').exists()).toBe(false)
        wrapper.unmount()
    })
})
