import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import AppTransport from '../AppTransport.vue'
import {
    nextRepeatMode,
    transportIcons,
    transportStateTokens,
    type TransportState,
} from '../transport'
import { createWaveMpI18n } from '@/i18n'

const idle: TransportState = {
    playback: 'idle',
    position: 0,
    duration: 0,
    shuffle: false,
    repeat: 'off',
    volume: 0.8,
    muted: false,
}

function mountTransport(state: Partial<TransportState> = {}, expanded = false) {
    return mount(AppTransport, {
        props: { state: { ...idle, ...state }, expanded },
        global: { plugins: [createWaveMpI18n('en'), createPinia()] },
    })
}

describe('transport logic', () => {
    it('cycles the repeat mode', () => {
        expect(nextRepeatMode('off')).toBe('all')
        expect(nextRepeatMode('all')).toBe('one')
        expect(nextRepeatMode('one')).toBe('off')
    })

    it('chooses the icons from the state', () => {
        expect(transportIcons({ playback: 'idle', repeat: 'off', muted: false })).toEqual({
            play: 'play',
            repeat: 'repeat',
            volume: 'volume',
        })
        expect(transportIcons({ playback: 'playing', repeat: 'one', muted: true })).toEqual({
            play: 'pause',
            repeat: 'repeat-one',
            volume: 'muted',
        })
        expect(transportIcons({ playback: 'paused', repeat: 'all', muted: false }).play).toBe(
            'play',
        )
    })

    it('builds the state tokens of the bar', () => {
        expect(transportStateTokens('idle', false)).toBe('idle')
        expect(transportStateTokens('playing', true)).toBe('playing expanded')
        expect(transportStateTokens('playing', false, true)).toBe('playing loading')
    })
})

describe('AppTransport', () => {
    it('disables playback without a track', () => {
        const wrapper = mountTransport()

        expect(wrapper.get('[data-part="transport"]').attributes('data-state')).toBe('idle')
        for (const part of ['btn-play', 'btn-stop', 'btn-prev', 'btn-next', 'btn-compact']) {
            expect(wrapper.get(`[data-part="${part}"]`).attributes('disabled')).toBeDefined()
        }
        expect(wrapper.get('[data-part="seek"]').attributes('data-state')).toBe('disabled')
        expect(wrapper.get('[data-part="btn-shuffle"]').attributes('disabled')).toBeUndefined()
        expect(wrapper.get('[data-part="now-playing-mini"]').attributes('data-state')).toBe('empty')
        expect(wrapper.get('[data-part="now-playing-title"]').text()).toBe('Nothing playing')
        expect(wrapper.get('[data-part="transport-message"]').attributes('data-state')).toBe(
            'empty',
        )
    })

    it('enables play, previous and next for a stopped track', () => {
        const wrapper = mountTransport({ trackId: 't1', title: 'Song' })

        for (const part of ['btn-play', 'btn-prev', 'btn-next']) {
            expect(wrapper.get(`[data-part="${part}"]`).attributes('disabled')).toBeUndefined()
        }
        expect(wrapper.get('[data-part="btn-stop"]').attributes('disabled')).toBeDefined()
    })

    it('shows loading and the notice of the player', () => {
        const wrapper = mountTransport({
            trackId: 't1',
            playback: 'playing',
            loading: true,
            message: 'unsupported',
        })
        const message = wrapper.get('[data-part="transport-message"]')

        expect(wrapper.get('[data-part="transport"]').attributes('data-state')).toBe(
            'playing loading',
        )
        expect(message.attributes('role')).toBe('status')
        expect(message.attributes('data-state')).toBe('unsupported')
        expect(message.text()).toContain('cannot be played')
    })

    it('shows the pause icon while playing', () => {
        const wrapper = mountTransport({
            playback: 'playing',
            trackId: 't1',
            title: 'Song',
            artist: 'Artist',
            duration: 200,
            position: 65,
        })
        const play = wrapper.get('[data-part="btn-play"]')

        expect(wrapper.get('[data-part="transport"]').attributes('data-state')).toContain('playing')
        expect(play.attributes('data-state')).toBe('playing')
        expect(play.attributes('disabled')).toBeUndefined()
        expect(play.get('[data-part="icon"]').attributes('data-icon')).toBe('pause')
        expect(play.get('[data-part="label"]').text()).toBe('Pause')
        expect(wrapper.get('[data-part="now-playing-artist"]').text()).toBe('Artist')
        expect(
            wrapper
                .get('[data-part="seek"] [data-part="slider-thumb"]')
                .attributes('aria-valuetext'),
        ).toBe('1:05 of 3:20')
    })

    it('shows repeat one and shuffle as pressed toggles', () => {
        const wrapper = mountTransport({ repeat: 'one', shuffle: true })
        const repeat = wrapper.get('[data-part="btn-repeat"]')

        expect(repeat.attributes('aria-pressed')).toBe('true')
        expect(repeat.attributes('data-state')).toBe('one')
        expect(repeat.get('[data-part="icon"]').attributes('data-icon')).toBe('repeat-one')
        expect(wrapper.get('[data-part="btn-shuffle"]').attributes('aria-pressed')).toBe('true')
        expect(wrapper.get('[data-part="btn-shuffle"]').attributes('data-state')).toBe('on')
    })

    it('shows the muted state', () => {
        const wrapper = mountTransport({ muted: true })
        const mute = wrapper.get('[data-part="btn-mute"]')

        expect(mute.attributes('aria-pressed')).toBe('true')
        expect(mute.attributes('data-state')).toBe('muted')
        expect(mute.get('[data-part="icon"]').attributes('data-icon')).toBe('muted')
        expect(wrapper.get('[data-part="volume"]').attributes('data-state')).toBe('muted')
    })

    it('marks the expanded full-screen player', () => {
        const wrapper = mountTransport({}, true)

        expect(wrapper.get('[data-part="transport"]').attributes('data-state')).toBe(
            'idle expanded',
        )
    })

    it('emits the controls', async () => {
        const wrapper = mountTransport({ trackId: 't1', playback: 'paused', duration: 100 })

        await wrapper.get('[data-part="btn-shuffle"]').trigger('click')
        await wrapper.get('[data-part="btn-repeat"]').trigger('click')
        await wrapper.get('[data-part="btn-mute"]').trigger('click')
        await wrapper.get('[data-part="volume"] [data-part="slider-thumb"]').trigger('keydown', {
            key: 'ArrowLeft',
        })
        await wrapper.get('[data-part="now-playing-mini"]').trigger('click')
        await wrapper.get('[data-part="btn-collapse"]').trigger('click')
        await wrapper.get('[data-part="btn-play"]').trigger('click')
        await wrapper.get('[data-part="btn-next"]').trigger('click')
        await wrapper.get('[data-part="btn-prev"]').trigger('click')
        await wrapper.get('[data-part="btn-stop"]').trigger('click')

        expect(wrapper.emitted('toggleShuffle')).toHaveLength(1)
        expect(wrapper.emitted('cycleRepeat')).toHaveLength(1)
        expect(wrapper.emitted('toggleMute')).toHaveLength(1)
        expect(wrapper.emitted('changeVolume')?.[0]).toEqual([0.75])
        // jsdom has no matchMedia, so the transport behaves like on mobile.
        expect(wrapper.emitted('expand')).toHaveLength(1)
        expect(wrapper.emitted('collapse')).toHaveLength(1)
        expect(wrapper.emitted('play')).toHaveLength(1)
        expect(wrapper.emitted('next')).toHaveLength(1)
        expect(wrapper.emitted('previous')).toHaveLength(1)
        expect(wrapper.emitted('stop')).toHaveLength(1)
    })
})
