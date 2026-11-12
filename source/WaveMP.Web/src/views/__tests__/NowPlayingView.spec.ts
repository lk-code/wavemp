import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import type { Track } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { getPlayback } from '@/player/api'
import { playerDependencies, usePlayerStore } from '@/stores/player'
import { useSessionStore } from '@/stores/session'
import NowPlayingView from '../NowPlayingView.vue'

vi.mock('@/library/api', () => ({ fetchAlbumCover: vi.fn(), fetchTrackCover: vi.fn() }))
vi.mock('@/player/api', () => ({
    getPlayback: vi.fn(),
    getVideoPlayback: vi.fn(),
    recordPlay: vi.fn(),
}))

function track(id: string): Track {
    return {
        id,
        title: id.toUpperCase(),
        hasAudio: true,
        hasCover: false,
        durationMilliseconds: 125_000,
        artist: { id: 'band', name: 'Band' },
        album: { id: 'album', title: 'Album', artist: { id: 'band', name: 'Band' } },
    }
}

const engine = {
    position: 0,
    hasPrepared: false,
    load: vi.fn(),
    prepare: vi.fn(),
    play: vi.fn(() => Promise.resolve()),
    pause: vi.fn(),
    stop: vi.fn(),
    seek: vi.fn(),
    setVolume: vi.fn(),
    setTrackGain: vi.fn(),
    setEqualizer: vi.fn(),
    fadeOut: vi.fn(),
    restoreOutput: vi.fn(),
    equalizerAvailable: true,
}

function mountView() {
    return mount(NowPlayingView, { global: { plugins: [createWaveMpI18n('en')] } })
}

function queueIds(wrapper: ReturnType<typeof mountView>): string[] {
    return wrapper.findAll('[data-part="queue-item-title"]').map((item) => item.text())
}

describe('NowPlayingView', () => {
    const original = { ...playerDependencies }

    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(getPlayback).mockImplementation((id) =>
            Promise.resolve({
                trackId: id,
                method: 'DirectPlay',
                streamUrl: `/stream/${id}`,
                replayGain: {},
            }),
        )
        playerDependencies.createEngine = () => engine
        playerDependencies.audioFormats = () => ['Mp3']
        playerDependencies.mediaSession = false
    })

    afterEach(() => {
        Object.assign(playerDependencies, original)
    })

    it('shows the empty state without a queue', () => {
        const wrapper = mountView()

        expect(wrapper.find('[data-part="empty-state"]').exists()).toBe(true)
        expect(wrapper.find('[data-part="queue"]').exists()).toBe(false)
    })

    it('shows the current song and the queue with its states', async () => {
        const player = usePlayerStore()
        player.playTracks([track('a'), track('b'), track('c')], 1)
        await flushPromises()
        const wrapper = mountView()

        expect(wrapper.get('[data-part="stage-title"]').text()).toBe('B')
        expect(wrapper.get('[data-part="stage-subtitle"]').text()).toBe('Band – Album')
        const items = wrapper.findAll('[data-part="queue-item"]')
        expect(items.map((item) => item.attributes('data-state'))).toEqual([
            'played',
            'current',
            undefined,
        ])
        expect(items[1]!.attributes('aria-current')).toBe('true')
        expect(items[0]!.get('[data-part="queue-item-duration"]').text()).toBe('2:05')
        expect(items[1]!.get('[data-item="remove"]').attributes('disabled')).toBeDefined()
    })

    it('switches the view of the stage without loading the song again', async () => {
        const session = useSessionStore()
        vi.spyOn(session, 'updatePlayerViewMode').mockImplementation((mode) => {
            session.playerViewMode = mode
            return Promise.resolve()
        })
        const player = usePlayerStore()
        player.playTracks([track('a')], 0)
        await flushPromises()
        engine.load.mockClear()
        const wrapper = mountView()

        await wrapper.get('[data-part="btn-view-visualization"]').trigger('click')
        expect(wrapper.get('[data-part="stage"]').attributes('data-state')).toBe('visualization')

        await wrapper.get('[data-part="btn-view-cover"]').trigger('click')
        expect(wrapper.get('[data-part="stage"]').attributes('data-state')).toBe('cover')
        expect(session.updatePlayerViewMode).toHaveBeenLastCalledWith('Cover')
        expect(engine.load).not.toHaveBeenCalled()
    })

    it('plays, moves, removes and clears entries', async () => {
        const player = usePlayerStore()
        player.playTracks([track('a'), track('b'), track('c')], 0)
        await flushPromises()
        const wrapper = mountView()

        await wrapper.findAll('[data-item="move-down"]')[1]!.trigger('click')
        expect(queueIds(wrapper)).toEqual(['A', 'C', 'B'])

        await wrapper.findAll('[data-item="remove"]')[1]!.trigger('click')
        expect(queueIds(wrapper)).toEqual(['A', 'B'])

        await wrapper.findAll('[data-part="queue-item-info"]')[1]!.trigger('click')
        await flushPromises()
        expect(player.state.trackId).toBe('b')

        await wrapper.get('[data-item="clear-queue"]').trigger('click')
        expect(queueIds(wrapper)).toEqual(['B'])
        expect(wrapper.get('[data-item="clear-queue"]').attributes('disabled')).toBeDefined()
    })
})
