import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { EngineEvents } from '../engine'
import { canFullscreen, enterFullscreen, exitFullscreen, isFullscreen } from '../fullscreen'
import type { HlsAttachment, HlsSupport } from '../hls'
import { VideoEngine } from '../videoEngine'
import {
    supportedVideoFormats,
    videoAudioCodecProbes,
    videoCodecProbes,
    videoContainerProbes,
} from '../videoFormats'

/** A video element without a browser: the tests drive time and events. */
class FakeVideo extends EventTarget {
    src = ''
    currentTime = 0
    duration = Number.NaN
    paused = true
    ended = false
    volume = 1
    error: MediaError | null = null
    readonly play = vi.fn(() => {
        this.paused = false
        this.dispatchEvent(new Event('playing'))
        return Promise.resolve()
    })
    readonly pause = vi.fn(() => {
        if (!this.paused) {
            this.paused = true
            this.dispatchEvent(new Event('pause'))
        }
    })
    readonly load = vi.fn()

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

    attach(_media: HTMLMediaElement, url: string): Promise<HlsAttachment> {
        this.urls.push(url)
        return Promise.resolve({ destroy: this.destroy })
    }
}

describe('VideoEngine', () => {
    let video: FakeVideo
    let hls: FakeHls
    let events: Required<EngineEvents>
    let engine: VideoEngine

    beforeEach(() => {
        video = new FakeVideo()
        hls = new FakeHls()
        events = {
            timeupdate: vi.fn(),
            playing: vi.fn(),
            paused: vi.fn(),
            waiting: vi.fn(),
            ended: vi.fn(),
            advanced: vi.fn(),
            error: vi.fn(),
        }
        engine = new VideoEngine(video as unknown as HTMLVideoElement, events, hls)
    })

    it('plays a video file and reports its time and end', async () => {
        engine.load({ url: '/video', gain: 1 })
        await engine.play()
        video.duration = 10
        video.currentTime = 4
        video.dispatchEvent(new Event('timeupdate'))
        video.ended = true
        video.dispatchEvent(new Event('ended'))

        expect(video.src).toBe('/video')
        expect(video.load).toHaveBeenCalledOnce()
        expect(events.playing).toHaveBeenCalledOnce()
        expect(events.timeupdate).toHaveBeenCalledWith(4, 10)
        expect(events.ended).toHaveBeenCalledOnce()
        expect(engine.hasPrepared).toBe(false)
    })

    it('attaches a transcoded video as HLS and detaches it when stopped', async () => {
        engine.load({ url: '/master.m3u8', gain: 1, hls: true })
        await Promise.resolve()

        expect(hls.urls).toEqual(['/master.m3u8'])
        expect(video.src).toBe('')

        engine.stop()

        expect(hls.destroy).toHaveBeenCalledOnce()
        expect(engine.source).toBeNull()
    })

    it('starts at the position once the metadata is known', () => {
        engine.load({ url: '/video', gain: 1 }, 42)
        video.dispatchEvent(new Event('loadedmetadata'))

        expect(video.currentTime).toBe(42)
    })

    it('reports nothing after it stopped', async () => {
        engine.load({ url: '/video', gain: 1 })
        await engine.play()

        engine.stop()
        video.dispatchEvent(new Event('error'))

        expect(video.src).toBe('')
        expect(events.paused).not.toHaveBeenCalled()
        expect(events.error).not.toHaveBeenCalled()
    })

    it('lets go of the element when disposed', () => {
        engine.load({ url: '/video', gain: 1 })
        engine.dispose()
        engine.load({ url: '/other', gain: 1 })
        video.dispatchEvent(new Event('playing'))

        expect(events.playing).not.toHaveBeenCalled()
    })

    it('applies volume and track gain to the element and clamps seeking', () => {
        engine.setVolume(0.5)
        engine.load({ url: '/video', gain: 0.5 })
        expect(video.volume).toBe(0.25)
        engine.setTrackGain(4)
        expect(video.volume).toBe(1)

        video.duration = 10
        engine.seek(20)
        expect(video.currentTime).toBe(10)
    })

    it('reports a rejected play but not an interrupted one', async () => {
        engine.load({ url: '/video', gain: 1 })
        video.play.mockRejectedValueOnce(new DOMException('aborted', 'AbortError'))
        await engine.play()
        expect(events.error).not.toHaveBeenCalled()

        video.play.mockRejectedValueOnce(new DOMException('blocked', 'NotAllowedError'))
        await engine.play()
        expect(events.error).toHaveBeenCalledOnce()
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
