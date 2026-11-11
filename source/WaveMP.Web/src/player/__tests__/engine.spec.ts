import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AudioEngine, handoverLead, type AudioEnvironment, type EngineEvents } from '../engine'

/** An audio element without a browser: the tests drive time and events. */
class FakeAudio extends EventTarget {
    src = ''
    currentTime = 0
    duration = Number.NaN
    paused = true
    ended = false
    volume = 1
    preload = ''
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

    advance(time: number, duration = 10): void {
        this.duration = duration
        this.currentTime = time
        this.dispatchEvent(new Event('timeupdate'))
    }

    end(): void {
        this.ended = true
        this.paused = true
        this.dispatchEvent(new Event('pause'))
        this.dispatchEvent(new Event('ended'))
    }
}

class FakeGain {
    readonly gain = { value: 1 }
    readonly connect = vi.fn()
}

class FakeContext {
    state = 'suspended'
    readonly destination = {}
    readonly gains: FakeGain[] = []
    readonly sources: { element: unknown; connect: ReturnType<typeof vi.fn> }[] = []
    readonly analyser = { connect: vi.fn() }
    readonly resume = vi.fn(() => {
        this.state = 'running'
        return Promise.resolve()
    })
    readonly close = vi.fn(() => Promise.resolve())

    createGain(): FakeGain {
        const gain = new FakeGain()
        this.gains.push(gain)
        return gain
    }

    createAnalyser() {
        return this.analyser
    }

    createMediaElementSource(element: unknown) {
        const source = { element, connect: vi.fn() }
        this.sources.push(source)
        return source
    }
}

interface Setup {
    engine: AudioEngine
    decks: FakeAudio[]
    events: Required<EngineEvents>
    timers: (() => void)[]
    context: FakeContext | null
}

function setup(withContext = false): Setup {
    const decks: FakeAudio[] = []
    const timers: (() => void)[] = []
    const context = withContext ? new FakeContext() : null
    const environment: AudioEnvironment = {
        createAudio: () => {
            const audio = new FakeAudio()
            decks.push(audio)
            return audio as unknown as HTMLAudioElement
        },
        createContext: () => context as unknown as AudioContext | null,
        setTimeout: (callback) => timers.push(callback),
        clearTimeout: (handle) => {
            timers[handle - 1] = () => undefined
        },
    }
    const events: Required<EngineEvents> = {
        timeupdate: vi.fn(),
        playing: vi.fn(),
        paused: vi.fn(),
        waiting: vi.fn(),
        ended: vi.fn(),
        advanced: vi.fn(),
        error: vi.fn(),
    }
    return { engine: new AudioEngine(events, environment), decks, events, timers, context }
}

describe('AudioEngine', () => {
    let s: Setup

    beforeEach(() => {
        s = setup()
    })

    it('loads and plays a track on the first deck', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()

        expect(s.decks[0]!.src).toBe('/a')
        expect(s.decks[0]!.play).toHaveBeenCalledOnce()
        expect(s.events.playing).toHaveBeenCalledOnce()
        expect(s.engine.source).toEqual({ url: '/a', gain: 1 })
    })

    it('seeks to the start position once the metadata is known', () => {
        s.engine.load({ url: '/a', gain: 1 }, 42)
        s.decks[0]!.dispatchEvent(new Event('loadedmetadata'))

        expect(s.decks[0]!.currentTime).toBe(42)
    })

    it('reports time, pause and the end', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()
        s.decks[0]!.advance(3, 10)
        s.engine.pause()
        s.decks[0]!.end()

        expect(s.events.timeupdate).toHaveBeenCalledWith(3, 10)
        expect(s.events.paused).toHaveBeenCalledOnce()
        expect(s.events.ended).toHaveBeenCalledOnce()
        expect(s.events.advanced).not.toHaveBeenCalled()
    })

    it('starts the prepared track right before the current one ends', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()
        s.engine.prepare({ url: '/b', gain: 0.5 })
        expect(s.decks[1]!.src).toBe('/b')
        expect(s.engine.hasPrepared).toBe(true)

        s.decks[0]!.advance(5, 10)
        expect(s.timers).toHaveLength(0)
        s.decks[0]!.advance(9.7, 10)
        expect(s.timers).toHaveLength(1)

        s.timers[0]!()

        expect(s.decks[1]!.play).toHaveBeenCalledOnce()
        expect(s.events.advanced).toHaveBeenCalledOnce()
        expect(s.engine.source).toEqual({ url: '/b', gain: 0.5 })
        expect(s.engine.hasPrepared).toBe(false)
        // The old deck plays its last moments and is unloaded afterwards.
        expect(s.decks[0]!.src).toBe('/a')
        s.decks[0]!.end()
        expect(s.events.ended).not.toHaveBeenCalled()
        s.timers[1]!()
        expect(s.decks[0]!.src).toBe('')
        expect(handoverLead).toBeLessThan(0.1)
    })

    it('takes over at the end when the timer did not fire', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()
        s.engine.prepare({ url: '/b', gain: 1 })

        s.decks[0]!.end()

        expect(s.events.advanced).toHaveBeenCalledOnce()
        expect(s.events.ended).not.toHaveBeenCalled()
        expect(s.decks[0]!.src).toBe('')
        expect(s.decks[1]!.play).toHaveBeenCalledOnce()
    })

    it('forgets the prepared track when another one is loaded', async () => {
        s.engine.prepare({ url: '/b', gain: 1 })
        s.engine.load({ url: '/c', gain: 1 })
        await s.engine.play()
        s.decks[0]!.end()

        expect(s.decks[1]!.src).toBe('')
        expect(s.events.ended).toHaveBeenCalledOnce()
    })

    it('stops and unloads both decks', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        s.engine.prepare({ url: '/b', gain: 1 })
        await s.engine.play()

        s.engine.stop()

        expect(s.decks.map((deck) => deck.src)).toEqual(['', ''])
        expect(s.engine.source).toBeNull()
    })

    it('clamps seeking to the track', () => {
        s.engine.load({ url: '/a', gain: 1 })
        s.decks[0]!.duration = 10

        s.engine.seek(20)
        expect(s.decks[0]!.currentTime).toBe(10)
        s.engine.seek(-5)
        expect(s.decks[0]!.currentTime).toBe(0)
    })

    it('sets volume and track gain on the elements without Web Audio', async () => {
        s.engine.setVolume(0.5)
        s.engine.load({ url: '/a', gain: 0.5 })
        await s.engine.play()
        expect(s.decks[0]!.volume).toBe(0.25)

        s.engine.setTrackGain(4)
        expect(s.decks[0]!.volume).toBe(1)
    })

    it('reports errors of the current deck only', () => {
        s.engine.load({ url: '/a', gain: 1 })
        s.engine.prepare({ url: '/b', gain: 1 })

        s.decks[1]!.dispatchEvent(new Event('error'))
        expect(s.events.error).not.toHaveBeenCalled()
        s.decks[0]!.dispatchEvent(new Event('error'))
        expect(s.events.error).toHaveBeenCalledOnce()
    })

    it('reports a rejected play', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        s.decks[0]!.play.mockRejectedValueOnce(new DOMException('blocked', 'NotAllowedError'))

        await s.engine.play()

        expect(s.events.error).toHaveBeenCalledOnce()
    })

    it('stays paused when paused while the audio context resumes', async () => {
        s = setup(true)
        let resume: () => void = () => undefined
        s.context!.resume.mockImplementationOnce(
            () =>
                new Promise<void>((resolve) => {
                    resume = resolve
                }),
        )
        s.engine.load({ url: '/a', gain: 1 })

        const playing = s.engine.play()
        s.engine.pause()
        resume()
        await playing

        expect(s.decks[0]!.play).not.toHaveBeenCalled()
    })

    it('routes both decks through their gains into the master with Web Audio', async () => {
        s = setup(true)
        s.engine.setVolume(0.3)
        s.engine.load({ url: '/a', gain: 0.5 })

        await s.engine.play()

        const context = s.context!
        expect(context.resume).toHaveBeenCalledOnce()
        expect(context.sources.map((source) => source.element)).toEqual(s.decks)
        const [master, deckA, deckB] = context.gains
        expect(master!.gain.value).toBe(0.3)
        expect(deckA!.gain.value).toBe(0.5)
        expect(deckB!.gain.value).toBe(1)
        expect(master!.connect).toHaveBeenCalledWith(context.analyser)
        expect(context.analyser.connect).toHaveBeenCalledWith(context.destination)
        expect(s.decks[0]!.volume).toBe(1)
        expect(s.engine.analyser).toBe(context.analyser)

        s.engine.setVolume(0.8)
        expect(master!.gain.value).toBe(0.8)
    })
})
