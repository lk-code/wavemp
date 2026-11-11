import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import AppTransport from '../AppTransport.vue'
import {
    nextRepeatMode,
    transportIcons,
    transportStateTokens,
    type TransportState,
} from '../transport'
import { createWaveMpI18n } from '@/i18n'
import { usePlaceholderTransport } from '@/composables/usePlaceholderTransport'

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
        global: { plugins: [createWaveMpI18n('en')] },
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
    })
})

describe('placeholder transport', () => {
    it('starts idle and changes only the UI state', () => {
        const transport = usePlaceholderTransport()

        expect(transport.state.playback).toBe('idle')
        expect(transport.state.volume).toBe(0.8)

        transport.toggleShuffle()
        transport.cycleRepeat()
        transport.toggleMute()
        expect(transport.state).toMatchObject({ shuffle: true, repeat: 'all', muted: true })

        transport.setVolume(0.5)
        expect(transport.state).toMatchObject({ volume: 0.5, muted: false })
    })
})

describe('AppTransport', () => {
    it('disables playback while idle', () => {
        const wrapper = mountTransport()

        expect(wrapper.get('[data-part="transport"]').attributes('data-state')).toBe('idle')
        for (const part of ['btn-play', 'btn-stop', 'btn-prev', 'btn-next', 'btn-compact']) {
            expect(wrapper.get(`[data-part="${part}"]`).attributes('disabled')).toBeDefined()
        }
        expect(wrapper.get('[data-part="seek"]').attributes('data-state')).toBe('disabled')
        expect(wrapper.get('[data-part="btn-shuffle"]').attributes('disabled')).toBeUndefined()
        expect(wrapper.get('[data-part="now-playing-mini"]').attributes('data-state')).toBe('empty')
        expect(wrapper.get('[data-part="now-playing-title"]').text()).toBe('Nothing playing')
    })

    it('shows the pause icon while playing', () => {
        const wrapper = mountTransport({
            playback: 'playing',
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
        const wrapper = mountTransport()

        await wrapper.get('[data-part="btn-shuffle"]').trigger('click')
        await wrapper.get('[data-part="btn-repeat"]').trigger('click')
        await wrapper.get('[data-part="btn-mute"]').trigger('click')
        await wrapper.get('[data-part="volume"] [data-part="slider-thumb"]').trigger('keydown', {
            key: 'ArrowLeft',
        })
        await wrapper.get('[data-part="now-playing-mini"]').trigger('click')
        await wrapper.get('[data-part="btn-collapse"]').trigger('click')

        expect(wrapper.emitted('toggleShuffle')).toHaveLength(1)
        expect(wrapper.emitted('cycleRepeat')).toHaveLength(1)
        expect(wrapper.emitted('toggleMute')).toHaveLength(1)
        expect(wrapper.emitted('changeVolume')?.[0]).toEqual([0.75])
        // jsdom has no matchMedia, so the transport behaves like on mobile.
        expect(wrapper.emitted('expand')).toHaveLength(1)
        expect(wrapper.emitted('collapse')).toHaveLength(1)
    })
})
