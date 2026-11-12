import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'
import type { Track, TrackPlayback, TrackVideoPlayback } from '@/api/generated/models'
import type { EngineEvents } from '@/player/engine'
import { getPlayback, getVideoPlayback, recordPlay } from '@/player/api'
import type { FollowerClock, VideoFollowerEvents } from '@/player/videoFollower'
import {
    playerDependencies,
    usePlayerStore,
    type PlayerEngine,
    type PlayerVideoFollower,
} from '../player'
import { useSessionStore } from '../session'

vi.mock('@/player/api', () => ({
    getPlayback: vi.fn(),
    getVideoPlayback: vi.fn(),
    recordPlay: vi.fn(),
}))

class FakeEngine implements PlayerEngine {
    position = 0
    hasPrepared = false
    loaded: { url: string; gain: number; position: number }[] = []
    prepared: ({ url: string; gain: number } | null)[] = []
    readonly load = vi.fn((source: { url: string; gain: number }, position = 0) => {
        this.loaded.push({ ...source, position })
    })
    readonly prepare = vi.fn((source: { url: string; gain: number } | null) => {
        this.prepared.push(source)
        this.hasPrepared = source !== null
    })
    readonly play = vi.fn(() => {
        this.events.playing?.()
        return Promise.resolve()
    })
    readonly pause = vi.fn()
    readonly stop = vi.fn()
    readonly seek = vi.fn()
    readonly setVolume = vi.fn()
    readonly setTrackGain = vi.fn()

    constructor(readonly events: EngineEvents) {}

    get lastUrl(): string | undefined {
        return this.loaded.at(-1)?.url
    }
}

class FakeFollower implements PlayerVideoFollower {
    readonly load = vi.fn()
    readonly unload = vi.fn()
    readonly setActive = vi.fn()
    readonly setOffset = vi.fn()
    readonly sync = vi.fn()
    readonly dispose = vi.fn()

    constructor(
        readonly clock: () => FollowerClock,
        readonly events: VideoFollowerEvents,
    ) {}
}

function track(id: string, extra: Partial<Track> = {}): Track {
    return {
        id,
        title: id.toUpperCase(),
        hasAudio: true,
        durationMilliseconds: 200_000,
        artist: { id: 'ar', name: 'Band' },
        ...extra,
    }
}

function direct(id: string, extra: Partial<TrackPlayback> = {}): TrackPlayback {
    return {
        trackId: id,
        method: 'DirectPlay',
        format: 'Mp3',
        contentType: 'audio/mpeg',
        streamUrl: `/stream/${id}`,
        durationMilliseconds: 200_000,
        replayGain: { trackGain: -6, trackPeak: 0.5 },
        ...extra,
    }
}

const abc = [track('a'), track('b'), track('c')]

/** The video playback of a track with a video file the browser plays. */
function video(id: string): TrackVideoPlayback {
    return {
        trackId: id,
        method: 'DirectPlay',
        protocol: 'File',
        streamUrl: `/video/${id}`,
        durationMilliseconds: 200_000,
    }
}

describe('player store', () => {
    let engine: FakeEngine
    const original = { ...playerDependencies }

    beforeEach(() => {
        setActivePinia(createPinia())
        window.localStorage.clear()
        vi.mocked(getPlayback).mockReset()
        vi.mocked(recordPlay).mockReset().mockResolvedValue(undefined)
        vi.mocked(getPlayback).mockImplementation((id) => Promise.resolve(direct(id)))
        playerDependencies.createEngine = (events) => (engine = new FakeEngine(events))
        playerDependencies.audioFormats = () => ['Mp3', 'Flac']
        playerDependencies.random = () => 0
        playerDependencies.mediaSession = false
    })

    afterEach(() => {
        Object.assign(playerDependencies, original)
    })

    it('starts idle without a track', () => {
        const player = usePlayerStore()

        expect(player.state).toMatchObject({ playback: 'idle', position: 0, shuffle: false })
        expect(player.state.trackId).toBeUndefined()
        expect(player.state.volume).toBe(0.8)
    })

    it('plays a list from the chosen track', async () => {
        const player = usePlayerStore()

        player.playTracks(abc, 1)
        await flushPromises()

        expect(getPlayback).toHaveBeenCalledWith('b', ['Mp3', 'Flac'])
        expect(engine.lastUrl).toBe('/stream/b')
        expect(engine.play).toHaveBeenCalledOnce()
        expect(player.state).toMatchObject({
            playback: 'playing',
            loading: false,
            trackId: 'b',
            title: 'B',
            artist: 'Band',
            duration: 200,
        })
        expect(engine.setVolume).toHaveBeenCalledWith(0.8 ** 2)
    })

    it('ignores the gain of the track while ReplayGain is off and applies it when on', async () => {
        const session = useSessionStore()
        const player = usePlayerStore()

        player.playTracks(abc, 0)
        await flushPromises()
        expect(engine.loaded[0]!.gain).toBe(1)

        session.replayGain = 'Track'
        await flushPromises()
        expect(engine.setTrackGain.mock.calls.at(-1)?.[0]).toBeCloseTo(10 ** (-6 / 20))
    })

    it('moves on to the next track at the end and stops after the last', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 1)
        await flushPromises()

        engine.events.ended?.()
        await flushPromises()
        expect(player.state.trackId).toBe('c')
        expect(engine.lastUrl).toBe('/stream/c')

        engine.events.ended?.()
        await flushPromises()
        expect(player.state.trackId).toBe('c')
        expect(player.state.playback).toBe('paused')
    })

    it('prepares the next track before the end and follows the engine to it', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        engine.events.timeupdate?.(100, 200)
        await flushPromises()
        expect(engine.prepared.filter(Boolean)).toEqual([])

        engine.events.timeupdate?.(185, 200)
        await flushPromises()
        expect(engine.prepared.at(-1)).toEqual({ url: '/stream/b', gain: 1 })

        engine.events.advanced?.()
        expect(player.state.trackId).toBe('b')
        expect(player.state.position).toBe(0)
        expect(engine.load).toHaveBeenCalledOnce()
    })

    it('repeats one track and the whole queue', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 2)
        await flushPromises()

        player.cycleRepeat()
        expect(player.state.repeat).toBe('all')
        engine.events.ended?.()
        await flushPromises()
        expect(player.state.trackId).toBe('a')

        player.cycleRepeat()
        expect(player.state.repeat).toBe('one')
        engine.events.ended?.()
        await flushPromises()
        expect(player.state.trackId).toBe('a')
        expect(engine.loaded.map((item) => item.url)).toEqual([
            '/stream/c',
            '/stream/a',
            '/stream/a',
        ])
    })

    it('skips with next and previous, and previous restarts a track that played a while', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        player.next()
        await flushPromises()
        expect(player.state.trackId).toBe('b')

        engine.events.timeupdate?.(30, 200)
        player.previous()
        expect(engine.seek).toHaveBeenCalledWith(0)
        expect(player.state.trackId).toBe('b')

        engine.events.timeupdate?.(1, 200)
        player.previous()
        await flushPromises()
        expect(player.state.trackId).toBe('a')

        player.previous()
        await flushPromises()
        expect(player.state.trackId).toBe('a')
    })

    it('pauses, resumes, seeks and stops', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        player.togglePlay()
        expect(engine.pause).toHaveBeenCalledOnce()
        expect(player.state.playback).toBe('paused')

        player.togglePlay()
        expect(player.state.playback).toBe('playing')

        player.seek(500)
        expect(engine.seek).toHaveBeenCalledWith(200)

        player.stop()
        expect(engine.stop).toHaveBeenCalledOnce()
        expect(player.state).toMatchObject({ playback: 'idle', position: 0, trackId: 'a' })

        player.play()
        await flushPromises()
        expect(engine.load).toHaveBeenCalledTimes(2)
        expect(player.state.playback).toBe('playing')
    })

    it('stops waiting for data when paused and ignores waiting while paused', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        engine.events.waiting?.()
        expect(player.state.loading).toBe(true)
        player.pause()
        expect(player.state).toMatchObject({ playback: 'paused', loading: false })

        engine.events.waiting?.()
        expect(player.state.loading).toBe(false)
    })

    it('skips tracks that need transcoding with a notice', async () => {
        vi.mocked(getPlayback).mockImplementation((id) =>
            Promise.resolve(
                id === 'a'
                    ? { trackId: id, method: 'Transcode', transcodeReason: 'format_unsupported' }
                    : direct(id),
            ),
        )
        const player = usePlayerStore()

        player.playTracks(abc, 0)
        await flushPromises()

        expect(player.state.trackId).toBe('b')
        expect(player.state.message).toBe('unsupported')
        expect(engine.lastUrl).toBe('/stream/b')
    })

    it('plays transcoded tracks as HLS', async () => {
        vi.mocked(getPlayback).mockImplementation((id) =>
            Promise.resolve({
                trackId: id,
                method: 'Transcode',
                transcodeReason: 'format_unsupported',
                protocol: 'Hls',
                streamUrl: `/hls/${id}/master.m3u8`,
            }),
        )
        const player = usePlayerStore()

        player.playTracks(abc, 0)
        await flushPromises()

        expect(player.state.trackId).toBe('a')
        expect(player.state.message).toBeNull()
        expect(engine.load).toHaveBeenCalledWith(
            { url: '/hls/a/master.m3u8', gain: 1, hls: true },
            0,
        )
    })

    describe('video', () => {
        let follower: FakeFollower
        let created: number
        const element = {} as HTMLVideoElement
        const withVideo = [
            track('a', { hasVideo: true }),
            track('b'),
            track('c', { hasVideo: true }),
        ]

        beforeEach(() => {
            // Replaced as soon as the store creates its audio engine.
            engine = new FakeEngine({})
            created = 0
            vi.mocked(getVideoPlayback).mockReset()
            vi.mocked(getVideoPlayback).mockImplementation((id) => Promise.resolve(video(id)))
            playerDependencies.createVideoFollower = (_element, clock, events) => {
                created++
                return (follower = new FakeFollower(clock, events))
            }
            playerDependencies.videoFormats = () => ({
                containers: ['Mp4'],
                videoCodecs: ['H264'],
                audioCodecs: ['Aac'],
            })
        })

        afterEach(() => {
            Object.defineProperty(document, 'visibilityState', {
                value: 'visible',
                configurable: true,
            })
        })

        it('plays the audio once and shows the muted video with its offset', async () => {
            vi.mocked(getVideoPlayback).mockResolvedValue({
                ...video('a'),
                offsetMilliseconds: 1500,
            })
            const player = usePlayerStore()
            player.attachVideo(element)

            player.playTracks(withVideo, 0)
            await flushPromises()

            expect(engine.load).toHaveBeenCalledOnce()
            expect(engine.lastUrl).toBe('/stream/a')
            expect(getVideoPlayback).toHaveBeenCalledWith('a', {
                containers: ['Mp4'],
                videoCodecs: ['H264'],
                audioCodecs: ['Aac'],
            })
            expect(follower.load).toHaveBeenCalledWith({ url: '/video/a' }, 1500)
            expect(follower.setActive).toHaveBeenLastCalledWith(true)
            expect(player.stageView).toBe('cover')

            follower.events.stateChange?.('ready')
            expect(player.videoState).toBe('ready')
            expect(player.stageView).toBe('video')
        })

        it('lets the video follow the position of the audio engine', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()

            engine.position = 42
            expect(follower.clock()).toEqual({ position: 42, playing: true })

            player.pause()
            expect(follower.clock().playing).toBe(false)
            expect(follower.sync).toHaveBeenCalled()
        })

        it('switches between video, cover and visualization without touching the audio', async () => {
            const session = useSessionStore()
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()
            follower.events.stateChange?.('ready')
            engine.events.timeupdate?.(30, 200)

            session.playerViewMode = 'Cover'
            await flushPromises()
            expect(player.stageView).toBe('cover')
            expect(follower.setActive).toHaveBeenLastCalledWith(false)

            session.playerViewMode = 'Visualization'
            await flushPromises()
            expect(player.stageView).toBe('visualization')

            session.playerViewMode = 'Video'
            await flushPromises()
            expect(player.stageView).toBe('video')
            expect(follower.setActive).toHaveBeenLastCalledWith(true)

            expect(engine.load).toHaveBeenCalledOnce()
            expect(engine.stop).not.toHaveBeenCalled()
            expect(engine.seek).not.toHaveBeenCalled()
            expect(engine.pause).not.toHaveBeenCalled()
            expect(follower.load).toHaveBeenCalledOnce()
            expect(getVideoPlayback).toHaveBeenCalledOnce()
            expect(player.state).toMatchObject({ playback: 'playing', position: 30 })
        })

        it('loads no video for a track without one', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)

            player.playTracks([track('b')], 0)
            await flushPromises()

            expect(getVideoPlayback).not.toHaveBeenCalled()
            expect(player.stageView).toBe('cover')
            expect(player.hasVideo).toBe(false)
        })

        it('loads no video while the stage is closed or the cover is chosen', async () => {
            const session = useSessionStore()
            session.playerViewMode = 'Cover'
            const player = usePlayerStore()
            player.playTracks(withVideo, 0)
            await flushPromises()

            player.attachVideo(element)
            await flushPromises()

            expect(getVideoPlayback).not.toHaveBeenCalled()
            expect(engine.lastUrl).toBe('/stream/a')
        })

        it('prepares the next track also while a video is shown', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()

            engine.events.timeupdate?.(185, 200)
            await flushPromises()

            expect(engine.prepared.at(-1)).toEqual({ url: '/stream/b', gain: 1 })
        })

        it('changes the video when the audio moves on to the next track', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks([withVideo[0]!, withVideo[2]!], 0)
            await flushPromises()
            engine.events.timeupdate?.(185, 200)
            await flushPromises()

            engine.events.advanced?.()
            await flushPromises()

            expect(player.state.trackId).toBe('c')
            expect(follower.unload).toHaveBeenCalled()
            expect(follower.load).toHaveBeenLastCalledWith({ url: '/video/c' }, 0)
            expect(engine.load).toHaveBeenCalledOnce()
        })

        it('keeps the audio playing when the stage closes', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()
            const closed = follower

            player.detachVideo(element)
            await flushPromises()

            expect(closed.dispose).toHaveBeenCalledOnce()
            expect(engine.load).toHaveBeenCalledOnce()
            expect(engine.stop).not.toHaveBeenCalled()
            expect(player.state.playback).toBe('playing')
            expect(player.videoState).toBe('idle')

            player.attachVideo(element)
            await flushPromises()
            expect(created).toBe(2)
            expect(follower.load).toHaveBeenCalledOnce()
        })

        it('requests a failed video once more and shows the cover after that', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()

            follower.events.stateChange?.('failed')
            await flushPromises()
            expect(getVideoPlayback).toHaveBeenCalledTimes(2)
            expect(follower.load).toHaveBeenCalledTimes(2)

            follower.events.stateChange?.('failed')
            await flushPromises()
            expect(getVideoPlayback).toHaveBeenCalledTimes(2)
            expect(player.videoState).toBe('failed')
            expect(player.stageView).toBe('cover')
            expect(player.state.playback).toBe('playing')
        })

        it('shows the cover when the video cannot be requested', async () => {
            vi.mocked(getVideoPlayback).mockRejectedValue({ responseStatusCode: 404 })
            const player = usePlayerStore()
            player.attachVideo(element)

            player.playTracks(withVideo, 0)
            await flushPromises()

            expect(player.videoState).toBe('failed')
            expect(follower.load).not.toHaveBeenCalled()
            expect(player.state.playback).toBe('playing')
        })

        it('saves the chosen view mode for the profile', () => {
            const session = useSessionStore()
            const update = vi.spyOn(session, 'updatePlayerViewMode').mockResolvedValue()
            const player = usePlayerStore()

            player.selectViewMode('Visualization')

            expect(update).toHaveBeenCalledWith('Visualization')
        })

        it('applies a new offset only to the video of the current track', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()

            player.applyVideoOffset('c', 500)
            expect(follower.setOffset).not.toHaveBeenCalled()

            player.applyVideoOffset('a', 500)
            expect(follower.setOffset).toHaveBeenCalledWith(500)
        })

        it('lets the own controls of the video pause and play the audio', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()

            follower.events.userPause?.()
            expect(engine.pause).toHaveBeenCalledOnce()
            expect(player.state.playback).toBe('paused')

            follower.events.userPlay?.()
            expect(player.state.playback).toBe('playing')
        })

        it('hides the video while the page is hidden', async () => {
            const player = usePlayerStore()
            player.attachVideo(element)
            player.playTracks(withVideo, 0)
            await flushPromises()

            Object.defineProperty(document, 'visibilityState', {
                value: 'hidden',
                configurable: true,
            })
            document.dispatchEvent(new Event('visibilitychange'))
            await flushPromises()
            expect(follower.setActive).toHaveBeenLastCalledWith(false)

            Object.defineProperty(document, 'visibilityState', {
                value: 'visible',
                configurable: true,
            })
            document.dispatchEvent(new Event('visibilitychange'))
            await flushPromises()
            expect(follower.setActive).toHaveBeenLastCalledWith(true)
            expect(follower.load).toHaveBeenCalledOnce()
        })
    })

    it('stops when no track of the queue can be played', async () => {
        vi.mocked(getPlayback).mockRejectedValue({ responseStatusCode: 404 })
        const player = usePlayerStore()

        player.playTracks(abc, 0)
        await flushPromises()

        expect(player.state.playback).toBe('idle')
        expect(player.state.message).toBe('unavailable')
    })

    it('loads a failed stream once more at its position before skipping', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()
        engine.events.timeupdate?.(42, 200)

        engine.events.error?.(new Error('expired'))
        await flushPromises()
        expect(player.state.trackId).toBe('a')
        expect(engine.loaded.at(-1)).toMatchObject({ url: '/stream/a', position: 42 })

        engine.events.error?.(new Error('broken'))
        await flushPromises()
        expect(player.state.trackId).toBe('b')
        expect(player.state.message).toBe('failed')
    })

    it('edits the queue', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        player.playNext([track('x')])
        player.addToQueue([track('y')])
        expect(player.queue.items.map((item) => item.track.id)).toEqual(['a', 'x', 'b', 'c', 'y'])

        player.moveItem(player.queue.items[4]!.key, -1)
        player.removeItem(player.queue.items[1]!.key)
        expect(player.queue.items.map((item) => item.track.id)).toEqual(['a', 'b', 'y', 'c'])

        player.playItem(player.queue.items[2]!.key)
        await flushPromises()
        expect(player.state.trackId).toBe('y')

        player.clearQueue()
        expect(player.queue.items.map((item) => item.track.id)).toEqual(['y'])
    })

    it('makes the first added track current without playing it', async () => {
        const player = usePlayerStore()

        player.addToQueue([track('a'), track('b')])
        await flushPromises()

        expect(player.state).toMatchObject({ trackId: 'a', playback: 'paused' })
        expect(engine.play).not.toHaveBeenCalled()
    })

    it('shuffles around the current track and restores the order', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 1)
        await flushPromises()

        player.toggleShuffle()
        expect(player.state.shuffle).toBe(true)
        expect(player.queue.items[0]!.track.id).toBe('b')

        player.toggleShuffle()
        expect(player.queue.items.map((item) => item.track.id)).toEqual(['a', 'b', 'c'])
        expect(player.state.trackId).toBe('b')
    })

    it('plays a list shuffled on request', async () => {
        const player = usePlayerStore()

        player.playTracks(abc, -1, { shuffle: true })
        await flushPromises()

        expect(player.state.shuffle).toBe(true)
        expect(player.queue.index).toBe(0)
    })

    it('keeps the volume of the device', async () => {
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        player.setVolume(0.5)
        expect(engine.setVolume).toHaveBeenLastCalledWith(0.25)
        player.toggleMute()
        expect(engine.setVolume).toHaveBeenLastCalledWith(0)

        setActivePinia(createPinia())
        expect(usePlayerStore().state).toMatchObject({ volume: 0.5, muted: true })
    })

    it('reports a play to the history after half the track', async () => {
        vi.mocked(getPlayback).mockResolvedValue(direct('a', { durationMilliseconds: 4000 }))
        const player = usePlayerStore()
        player.playTracks([track('a', { durationMilliseconds: 4000 })], 0)
        await flushPromises()

        for (let time = 0; time <= 2.5; time += 0.25) {
            engine.events.timeupdate?.(time, 4)
        }
        await flushPromises()

        expect(recordPlay).toHaveBeenCalledOnce()
        expect(vi.mocked(recordPlay).mock.calls[0]![0]).toBe('a')
    })

    it('empties the player when the session changes', async () => {
        const session = useSessionStore()
        const player = usePlayerStore()
        player.playTracks(abc, 0)
        await flushPromises()

        session.token = 'other'
        await flushPromises()

        expect(engine.stop).toHaveBeenCalled()
        expect(player.queue.items).toEqual([])
        expect(player.state.playback).toBe('idle')
    })
})
