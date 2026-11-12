import { beforeEach, describe, expect, it, vi } from 'vitest'
import { canFullscreen, enterFullscreen, exitFullscreen, isFullscreen } from '../fullscreen'
import type { HlsAttachment, HlsSupport } from '../hls'
import {
    syncInterval,
    VideoFollower,
    type FollowerClock,
    type FollowerEnvironment,
    type VideoFollowerEvents,
} from '../videoFollower'
import {
    supportedVideoFormats,
    videoAudioCodecProbes,
    videoCodecProbes,
    videoContainerProbes,
} from '../videoFormats'

/** A video element without a browser: the tests drive time, seeking and events. */
class FakeVideo extends EventTarget {
    src = ''
    duration = Number.NaN
    paused = true
    ended = false
    muted = false
    defaultMuted = false
    playsInline = false
    playbackRate = 1
    readyState = 0
    seeking = false
    error: MediaError | null = null
    private time = 0
    readonly play = vi.fn(() => {
        if (this.paused) {
            this.paused = false
            this.dispatchEvent(new Event('play'))
            this.dispatchEvent(new Event('playing'))
        }
        return Promise.resolve()
    })
    readonly pause = vi.fn(() => {
        if (!this.paused) {
            this.paused = true
            this.dispatchEvent(new Event('pause'))
        }
    })
    readonly load = vi.fn()

    get currentTime(): number {
        return this.time
    }

    /** Setting the position starts a seek, which the test finishes with seeked(). */
    set currentTime(value: number) {
        this.time = value
        this.seeking = true
    }

    /** The metadata and the first picture arrived. */
    loadMetadata(duration = 200): void {
        this.duration = duration
        this.readyState = 1
        this.dispatchEvent(new Event('loadedmetadata'))
    }

    loadData(): void {
        this.readyState = 4
        this.dispatchEvent(new Event('loadeddata'))
    }

    seeked(): void {
        this.seeking = false
        this.readyState = 4
        this.dispatchEvent(new Event('seeked'))
    }

    /** Plays on by itself, without a seek. */
    advance(seconds: number): void {
        this.time += seconds
    }

    getAttribute(name: string): string | null {
        return name === 'src' && this.src !== '' ? this.src : null
    }

    removeAttribute(name: string): void {
        if (name === 'src') {
            this.src = ''
        }
    }
}

class FakeHls implements HlsSupport {
    readonly urls: string[] = []
    readonly destroy = vi.fn()
    fail: ((error: unknown) => void) | null = null

    attach(
        _media: HTMLMediaElement,
        url: string,
        onFatalError: (error: unknown) => void,
    ): Promise<HlsAttachment> {
        this.urls.push(url)
        this.fail = onFatalError
        return Promise.resolve({ destroy: this.destroy })
    }
}

describe('VideoFollower', () => {
    let video: FakeVideo
    let hls: FakeHls
    let clock: { position: number; playing: boolean }
    let now: number
    let ownControls: boolean
    let ticks: Map<number, () => void>
    let events: Required<VideoFollowerEvents> & { states: string[] }
    let follower: VideoFollower

    function tick(): void {
        for (const callback of ticks.values()) {
            callback()
        }
    }

    beforeEach(() => {
        video = new FakeVideo()
        hls = new FakeHls()
        clock = { position: 30, playing: true }
        now = 100_000
        ownControls = false
        ticks = new Map()
        let handles = 0
        const environment: FollowerEnvironment = {
            setInterval: (callback, delay) => {
                expect(delay).toBe(syncInterval)
                ticks.set(++handles, callback)
                return handles
            },
            clearInterval: (handle) => ticks.delete(handle),
            now: () => now,
            hls,
            hasOwnControls: () => ownControls,
        }
        const states: string[] = []
        events = {
            states,
            stateChange: vi.fn((state) => states.push(state)),
            userPlay: vi.fn(),
            userPause: vi.fn(),
        }
        follower = new VideoFollower(
            video as unknown as HTMLVideoElement,
            (): FollowerClock => clock,
            events,
            environment,
        )
    })

    /** Loads and shows a video at the position of the audio. */
    function loadShown(offset = 0): void {
        follower.load({ url: '/video' }, offset)
        follower.setActive(true)
        video.loadMetadata()
        video.seeked()
    }

    it('keeps the video muted', () => {
        expect(video.muted).toBe(true)
        expect(video.defaultMuted).toBe(true)
        expect(video.playsInline).toBe(true)
    })

    it('positions a loaded video at the audio plus the offset and shows it when it got there', () => {
        follower.load({ url: '/video' }, 1500)
        follower.setActive(true)
        expect(follower.videoState).toBe('loading')

        video.loadMetadata()
        expect(video.currentTime).toBe(31.5)
        expect(follower.videoState).toBe('loading')

        video.seeked()
        expect(follower.videoState).toBe('ready')
        expect(events.states).toEqual(['loading', 'ready'])
        expect(video.play).toHaveBeenCalled()
        expect(video.src).toBe('/video')
    })

    it('shows a video that needs no seek once its first picture arrived', () => {
        clock.position = 0
        follower.load({ url: '/video' })
        follower.setActive(true)
        video.loadMetadata()
        expect(video.seeking).toBe(false)

        video.loadData()

        expect(follower.videoState).toBe('ready')
    })

    it('plays and pauses like the audio', () => {
        loadShown()
        expect(video.paused).toBe(false)

        clock.playing = false
        follower.sync()
        expect(video.paused).toBe(true)

        clock.playing = true
        follower.sync()
        expect(video.paused).toBe(false)
        expect(events.userPause).not.toHaveBeenCalled()
        expect(events.userPlay).not.toHaveBeenCalled()
    })

    it('corrects small differences by the speed and large ones by a seek', () => {
        loadShown()
        clock.position = 30.1
        tick()
        expect(video.playbackRate).toBeCloseTo(1.05)
        expect(video.seeking).toBe(false)

        now += 5000
        clock.position = 35
        tick()
        expect(video.currentTime).toBe(35)
        expect(video.playbackRate).toBe(1)
        expect(follower.videoState).toBe('ready')
    })

    it('waits for a running seek before it checks again', () => {
        loadShown()
        now += 5000
        clock.position = 40
        follower.sync()
        clock.position = 50
        follower.sync()

        expect(video.currentTime).toBe(40)
    })

    it('holds the first picture until the music of the video starts', () => {
        clock.position = 1
        follower.load({ url: '/video' }, -3000)
        follower.setActive(true)
        video.loadMetadata()
        video.loadData()

        expect(video.paused).toBe(true)
        expect(follower.videoState).toBe('ready')

        clock.position = 3.05
        tick()
        expect(video.paused).toBe(false)
    })

    it('pauses and stops checking while hidden and positions again when shown', () => {
        loadShown()
        follower.setActive(false)

        expect(video.paused).toBe(true)
        expect(ticks.size).toBe(0)
        expect(video.src).toBe('/video')

        now += 5000
        clock.position = 60
        follower.setActive(true)

        expect(video.currentTime).toBe(60)
        expect(follower.videoState).toBe('loading')
        video.seeked()
        expect(follower.videoState).toBe('ready')
        expect(ticks.size).toBe(1)
    })

    it('stays ready when shown again at the right position', () => {
        loadShown()
        follower.setActive(false)
        video.advance(0.01)

        follower.setActive(true)

        expect(follower.videoState).toBe('ready')
    })

    it('applies a new offset with the next check', () => {
        loadShown()
        now += 5000

        follower.setOffset(2000)
        follower.sync()

        expect(video.currentTime).toBe(32)
    })

    it('removes the source when unloaded', () => {
        loadShown()

        follower.unload()

        expect(video.src).toBe('')
        expect(follower.videoState).toBe('idle')
        expect(ticks.size).toBe(0)
    })

    it('fails on an error of the element, but not after it was unloaded', () => {
        loadShown()
        video.dispatchEvent(new Event('error'))
        expect(follower.videoState).toBe('failed')

        follower.unload()
        video.dispatchEvent(new Event('error'))
        expect(follower.videoState).toBe('idle')
    })

    it('attaches a transcoded video as HLS and fails on a fatal HLS error', async () => {
        follower.load({ url: '/master.m3u8', hls: true })
        await Promise.resolve()

        expect(hls.urls).toEqual(['/master.m3u8'])
        expect(video.src).toBe('')

        hls.fail?.(new Error('fatal'))
        expect(follower.videoState).toBe('failed')

        follower.unload()
        expect(hls.destroy).toHaveBeenCalledOnce()
    })

    it('passes play and pause of the own controls of the video on to the audio', () => {
        loadShown()
        ownControls = true

        video.pause()
        expect(events.userPause).toHaveBeenCalledOnce()

        video.play()
        expect(events.userPlay).toHaveBeenCalledOnce()
    })

    it('ignores pauses of the browser outside of the own controls', () => {
        loadShown()

        video.pause()

        expect(events.userPause).not.toHaveBeenCalled()
        tick()
        expect(video.paused).toBe(false)
    })

    it('lets go of the element when disposed', () => {
        loadShown()
        follower.dispose()
        video.dispatchEvent(new Event('error'))

        expect(video.src).toBe('')
        expect(events.states.at(-1)).toBe('idle')
        expect(ticks.size).toBe(0)
    })
})

describe('video formats', () => {
    it('tests containers and codecs separately', () => {
        const playable = new Set([
            videoContainerProbes.Mp4,
            videoContainerProbes.Webm,
            videoCodecProbes.H264,
            videoCodecProbes.Vp9,
            videoAudioCodecProbes.Aac,
            videoAudioCodecProbes.Opus,
        ])

        const formats = supportedVideoFormats((type) => (playable.has(type) ? 'maybe' : ''))

        expect(formats).toEqual({
            containers: ['Mp4', 'Webm'],
            videoCodecs: ['H264', 'Vp9'],
            audioCodecs: ['Aac', 'Opus'],
        })
    })
})

describe('fullscreen', () => {
    function stage(enabled: boolean) {
        const container = document.createElement('div')
        const video = document.createElement('video')
        container.append(video)
        document.body.append(container)
        Object.defineProperty(document, 'fullscreenEnabled', { value: enabled, configurable: true })
        return { container, video }
    }

    it('shows the whole stage with the Fullscreen API', async () => {
        const { container, video } = stage(true)
        let current: Element | null = null
        Object.defineProperty(document, 'fullscreenElement', {
            get: () => current,
            configurable: true,
        })
        container.requestFullscreen = vi.fn(() => {
            current = container
            return Promise.resolve()
        })
        document.exitFullscreen = vi.fn(() => {
            current = null
            return Promise.resolve()
        })

        expect(canFullscreen(container, video)).toBe(true)
        await enterFullscreen(container, video)
        expect(isFullscreen(container, video)).toBe(true)
        await exitFullscreen(container, video)
        expect(isFullscreen(container, video)).toBe(false)
    })

    it('shows only the video where the Fullscreen API is missing', async () => {
        const { container, video } = stage(false)
        let displaying = false
        Object.assign(video, {
            webkitEnterFullscreen: vi.fn(() => (displaying = true)),
            webkitExitFullscreen: vi.fn(() => (displaying = false)),
        })
        Object.defineProperty(video, 'webkitDisplayingFullscreen', { get: () => displaying })

        expect(canFullscreen(container, video)).toBe(true)
        await enterFullscreen(container, video)
        expect(isFullscreen(container, video)).toBe(true)
        await exitFullscreen(container, video)
        expect(isFullscreen(container, video)).toBe(false)
    })

    it('is not available without either', () => {
        const { container, video } = stage(false)

        expect(canFullscreen(container, video)).toBe(false)
    })
})
