import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
    AudioEngine,
    crossfadeUnloadDelay,
    handoverLead,
    type AudioEnvironment,
    type EngineEvents,
} from '../engine'
import type { HlsAttachment, HlsSupport } from '../hls'

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

/** HLS support that records attachments; the tests resolve or reject them. */
class FakeHls implements HlsSupport {
    readonly attached: {
        media: HTMLMediaElement
        url: string
        onFatalError: (error: unknown) => void
        resolve: (attachment: HlsAttachment) => void
        reject: (error: unknown) => void
        destroy: ReturnType<typeof vi.fn<() => void>>
    }[] = []

    attach(
        media: HTMLMediaElement,
        url: string,
        onFatalError: (error: unknown) => void,
    ): Promise<HlsAttachment> {
        return new Promise((resolve, reject) => {
            this.attached.push({
                media,
                url,
                onFatalError,
                resolve,
                reject,
                destroy: vi.fn<() => void>(),
            })
        })
    }

    /** Completes the attachment with the given index. */
    async complete(index: number): Promise<void> {
        const entry = this.attached[index]!
        entry.resolve({ destroy: entry.destroy })
        await flush()
    }
}

async function flush(): Promise<void> {
    await Promise.resolve()
    await Promise.resolve()
}

/** An AudioParam that takes the target of every automation at once, so the tests can read it. */
class FakeParam {
    constructor(public value: number) {}

    readonly setTargetAtTime = vi.fn((target: number) => {
        this.value = target
    })
    readonly setValueAtTime = vi.fn((value: number) => {
        this.value = value
    })
    readonly linearRampToValueAtTime = vi.fn()
    readonly cancelScheduledValues = vi.fn()
    readonly setValueCurveAtTime = vi.fn()
}

class FakeGain {
    readonly gain = new FakeParam(1)
    readonly connect = vi.fn()
}

class FakeFilter {
    type = 'lowpass'
    readonly frequency = new FakeParam(350)
    readonly Q = new FakeParam(1)
    readonly gain = new FakeParam(0)
    readonly connect = vi.fn()
}

class FakeContext {
    state = 'suspended'
    currentTime = 100
    readonly destination = {}
    readonly gains: FakeGain[] = []
    readonly filters: FakeFilter[] = []
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

    createBiquadFilter(): FakeFilter {
        const filter = new FakeFilter()
        this.filters.push(filter)
        return filter
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
    delays: number[]
    context: FakeContext | null
    hls: FakeHls
}

function setup(withContext = false): Setup {
    const hls = new FakeHls()
    const decks: FakeAudio[] = []
    const timers: (() => void)[] = []
    const delays: number[] = []
    const context = withContext ? new FakeContext() : null
    const environment: AudioEnvironment = {
        createAudio: () => {
            const audio = new FakeAudio()
            decks.push(audio)
            return audio as unknown as HTMLAudioElement
        },
        createContext: () => context as unknown as AudioContext | null,
        webAudio: withContext,
        setTimeout: (callback, delay) => {
            delays.push(delay)
            return timers.push(callback)
        },
        clearTimeout: (handle) => {
            timers[handle - 1] = () => undefined
        },
        hls,
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
    return {
        engine: new AudioEngine(events, environment),
        decks,
        events,
        timers,
        delays,
        context,
        hls,
    }
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

    it('attaches HLS sources through the HLS support instead of the src', async () => {
        s.engine.load({ url: '/master.m3u8', gain: 1, hls: true })

        expect(s.hls.attached).toHaveLength(1)
        expect(s.hls.attached[0]!.url).toBe('/master.m3u8')
        expect(s.hls.attached[0]!.media).toBe(s.decks[0])
        expect(s.decks[0]!.src).toBe('')
        expect(s.decks[0]!.load).not.toHaveBeenCalled()

        await s.hls.complete(0)
        s.engine.stop()

        expect(s.hls.attached[0]!.destroy).toHaveBeenCalledOnce()
    })

    it('destroys an HLS attachment that completes after another track was loaded', async () => {
        s.engine.load({ url: '/master.m3u8', gain: 1, hls: true })
        s.engine.load({ url: '/b', gain: 1 })

        await s.hls.complete(0)

        expect(s.hls.attached[0]!.destroy).toHaveBeenCalledOnce()
        expect(s.decks[0]!.src).toBe('/b')
    })

    it('reports HLS errors of the current deck only', async () => {
        s.engine.load({ url: '/a.m3u8', gain: 1, hls: true })
        s.engine.prepare({ url: '/b.m3u8', gain: 1, hls: true })

        s.hls.attached[1]!.onFatalError(new Error('fatal'))
        expect(s.events.error).not.toHaveBeenCalled()
        s.hls.attached[0]!.reject(new Error('no HLS'))
        await flush()
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
        const graph = graphOf(context)
        expect(graph.master.gain.value).toBe(0.3)
        expect(graph.deckGains[0]!.gain.value).toBe(0.5)
        expect(graph.deckGains[1]!.gain.value).toBe(1)
        expect(graph.master.connect).toHaveBeenCalledWith(context.analyser)
        expect(context.analyser.connect).toHaveBeenCalledWith(context.destination)
        expect(s.decks[0]!.volume).toBe(1)
        expect(s.engine.analyser).toBe(context.analyser)

        s.engine.setVolume(0.8)
        expect(graph.master.gain.value).toBe(0.8)
    })

    it('routes the decks through their fades, the equalizer and the output fade', async () => {
        s = setup(true)
        s.engine.load({ url: '/a', gain: 1 })

        await s.engine.play()

        const context = s.context!
        const graph = graphOf(context)
        expect(context.sources[0]!.connect).toHaveBeenCalledWith(graph.deckGains[0])
        expect(graph.deckGains[0]!.connect).toHaveBeenCalledWith(graph.fades[0])
        expect(graph.fades[0]!.connect).toHaveBeenCalledWith(graph.preamp)
        expect(graph.fades[1]!.connect).toHaveBeenCalledWith(graph.preamp)
        expect(graph.preamp.connect).toHaveBeenCalledWith(context.filters[0])
        context.filters.slice(0, -1).forEach((filter, index) => {
            expect(filter.connect).toHaveBeenCalledWith(context.filters[index + 1])
        })
        expect(context.filters.at(-1)!.connect).toHaveBeenCalledWith(graph.outputFade)
        expect(graph.outputFade.connect).toHaveBeenCalledWith(graph.master)
    })

    it('builds ten filters with the frequencies and types of the bands', async () => {
        s = setup(true)
        s.engine.load({ url: '/a', gain: 1 })

        await s.engine.play()

        const filters = s.context!.filters
        expect(filters.map((filter) => filter.frequency.value)).toEqual([
            31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000,
        ])
        expect(filters[0]!.type).toBe('lowshelf')
        expect(filters.slice(1, -1).every((filter) => filter.type === 'peaking')).toBe(true)
        expect(filters[9]!.type).toBe('highshelf')
    })

    it('applies an equalizer set before the graph once it is built', async () => {
        s = setup(true)
        s.engine.setEqualizer({
            enabled: true,
            preampDb: -6,
            gainsDb: [6, 0, 0, 0, 0, 0, 0, 0, 0, -3],
        })
        s.engine.load({ url: '/a', gain: 1 })

        await s.engine.play()

        const filters = s.context!.filters
        expect(filters[0]!.gain.value).toBe(6)
        expect(filters[9]!.gain.value).toBe(-3)
        expect(graphOf(s.context!).preamp.gain.value).toBeCloseTo(0.501, 3)
    })

    it('changes the equalizer smoothly during playback', async () => {
        s = setup(true)
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()

        s.engine.setEqualizer({
            enabled: true,
            preampDb: 0,
            gainsDb: [0, 0, 0, 0, 0, 4, 0, 0, 0, 0],
        })

        const band = s.context!.filters[5]!
        expect(band.gain.setTargetAtTime).toHaveBeenLastCalledWith(4, 100, 0.02)
        expect(band.gain.value).toBe(4)
    })

    it('lets the signal through unchanged when the equalizer is off', async () => {
        s = setup(true)
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()
        s.engine.setEqualizer({
            enabled: true,
            preampDb: 6,
            gainsDb: [9, 9, 9, 9, 9, 9, 9, 9, 9, 9],
        })

        s.engine.setEqualizer({
            enabled: false,
            preampDb: 6,
            gainsDb: [9, 9, 9, 9, 9, 9, 9, 9, 9, 9],
        })

        expect(s.context!.filters.every((filter) => filter.gain.value === 0)).toBe(true)
        expect(graphOf(s.context!).preamp.gain.value).toBe(1)
    })

    it('reports the equalizer as unavailable without Web Audio', () => {
        expect(s.engine.equalizerAvailable).toBe(false)
        expect(setup(true).engine.equalizerAvailable).toBe(true)
    })

    describe('crossfade', () => {
        beforeEach(async () => {
            s = setup(true)
            s.engine.load({ url: '/a', gain: 1 })
            await s.engine.play()
        })

        it('starts the next track the length of the crossfade before the end', () => {
            s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })

            s.decks[0]!.advance(20, 30)
            expect(s.timers).toHaveLength(0)
            s.decks[0]!.advance(24.5, 30)
            expect(s.timers).toHaveLength(1)
            expect(s.delays[0]).toBeCloseTo((30 - 24.5 - 5 - handoverLead) * 1000, 5)
        })

        it('plays both tracks with equal-power curves and unloads the old one afterwards', () => {
            s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })
            s.decks[0]!.advance(24.6, 30)
            s.decks[0]!.currentTime = 25

            s.timers[0]!()

            const graph = graphOf(s.context!)
            expect(s.events.advanced).toHaveBeenCalledOnce()
            expect(s.engine.crossfading).toBe(true)
            expect(s.decks[1]!.play).toHaveBeenCalledOnce()
            expect(s.decks[0]!.paused).toBe(false)
            const [fadeIn, start, duration] = graph.fades[1]!.gain.setValueCurveAtTime.mock
                .calls[0] as [Float32Array, number, number]
            expect(fadeIn[0]).toBe(0)
            expect(fadeIn.at(-1)).toBeCloseTo(1)
            expect(start).toBe(100)
            expect(duration).toBe(5)
            const [fadeOut] = graph.fades[0]!.gain.setValueCurveAtTime.mock.calls[0] as [
                Float32Array,
            ]
            expect(fadeOut[0]).toBe(1)
            expect(fadeOut.at(-1)).toBeCloseTo(0)
            expect(s.delays.at(-1)).toBe((5 + crossfadeUnloadDelay) * 1000)

            s.timers.at(-1)!()

            expect(s.engine.crossfading).toBe(false)
            expect(s.decks[0]!.src).toBe('')
            expect(s.decks[1]!.src).toBe('/b')
        })

        it('shortens the crossfade to the rest of the track when the timer comes late', () => {
            s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })
            s.decks[0]!.advance(24.6, 30)
            s.decks[0]!.currentTime = 28

            s.timers[0]!()

            const [, , duration] = graphOf(s.context!).fades[1]!.gain.setValueCurveAtTime.mock
                .calls[0] as [Float32Array, number, number]
            expect(duration).toBe(2)
        })

        it('ends the crossfade when paused or seeking', () => {
            s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })
            s.decks[0]!.advance(25, 30)
            s.timers[0]!()

            s.engine.seek(3)

            expect(s.engine.crossfading).toBe(false)
            expect(s.decks[0]!.src).toBe('')
            expect(s.decks[0]!.pause).toHaveBeenCalled()
            expect(graphOf(s.context!).fades[1]!.gain.value).toBe(1)
        })

        it('loads a track prepared during the crossfade once it ended', () => {
            s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })
            s.decks[0]!.advance(25, 30)
            s.timers[0]!()

            s.engine.prepare({ url: '/c', gain: 1 })

            expect(s.decks[0]!.src).toBe('/a')
            expect(s.engine.hasPrepared).toBe(true)
            s.decks[0]!.end()
            expect(s.engine.crossfading).toBe(false)
            expect(s.decks[0]!.src).toBe('/c')
            expect(s.events.ended).not.toHaveBeenCalled()
        })

        it('changes gapless after an end without a timer', () => {
            s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })

            s.decks[0]!.end()

            expect(s.engine.crossfading).toBe(false)
            expect(s.decks[0]!.src).toBe('')
            expect(graphOf(s.context!).fades[1]!.gain.setValueCurveAtTime).not.toHaveBeenCalled()
        })
    })

    it('changes gapless without Web Audio even with a crossfade', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()
        s.engine.prepare({ url: '/b', gain: 1, crossfade: 5 })

        s.decks[0]!.advance(25, 30)
        expect(s.timers).toHaveLength(0)
        s.decks[0]!.advance(29.6, 30)
        s.timers[0]!()

        expect(s.engine.crossfading).toBe(false)
        expect(s.events.advanced).toHaveBeenCalledOnce()
    })

    it('fades the output out and back with Web Audio', async () => {
        s = setup(true)
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()
        const outputFade = graphOf(s.context!).outputFade.gain

        s.engine.fadeOut(10)
        expect(outputFade.linearRampToValueAtTime).toHaveBeenCalledWith(0, 110)

        s.engine.restoreOutput()
        expect(outputFade.cancelScheduledValues).toHaveBeenLastCalledWith(100)
        expect(outputFade.value).toBe(1)
    })

    it('fades the output out in steps of the element volume without Web Audio', async () => {
        s.engine.load({ url: '/a', gain: 1 })
        await s.engine.play()

        s.engine.fadeOut(1)
        for (let step = 0; step < 5; step++) {
            s.timers.at(-1)!()
        }
        expect(s.decks[0]!.volume).toBeCloseTo(0.5)
        for (let step = 0; step < 5; step++) {
            s.timers.at(-1)!()
        }
        expect(s.decks[0]!.volume).toBe(0)

        s.engine.restoreOutput()
        expect(s.decks[0]!.volume).toBe(1)
    })
})

interface Graph {
    master: FakeGain
    outputFade: FakeGain
    preamp: FakeGain
    deckGains: FakeGain[]
    fades: FakeGain[]
}

/** The gains in the order the engine creates them. */
function graphOf(context: FakeContext): Graph {
    const [master, outputFade, preamp, gainA, fadeA, gainB, fadeB] = context.gains
    return {
        master: master!,
        outputFade: outputFade!,
        preamp: preamp!,
        deckGains: [gainA!, gainB!],
        fades: [fadeA!, fadeB!],
    }
}
