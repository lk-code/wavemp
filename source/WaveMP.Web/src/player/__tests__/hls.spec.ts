import { beforeEach, describe, expect, it, vi } from 'vitest'
import { browserHlsSupport, hlsContentType } from '../hls'

/** An hls.js instance of the fake below. */
interface FakeHlsInstance {
    readonly handlers: Map<string, (event: string, data: unknown) => void>
    readonly loadSource: ReturnType<typeof vi.fn>
    readonly attachMedia: ReturnType<typeof vi.fn>
    readonly destroy: ReturnType<typeof vi.fn>
}

const fake = vi.hoisted(() => ({
    state: { supported: true, instances: [] as FakeHlsInstance[] },
}))

vi.mock('hls.js', () => {
    class Hls {
        static Events = { ERROR: 'hlsError' }
        static isSupported = () => fake.state.supported
        readonly handlers = new Map<string, (event: string, data: unknown) => void>()
        readonly loadSource = vi.fn()
        readonly attachMedia = vi.fn()
        readonly destroy = vi.fn()

        constructor(readonly config: unknown) {
            fake.state.instances.push(this)
        }

        on(event: string, handler: (event: string, data: unknown) => void): void {
            this.handlers.set(event, handler)
        }
    }

    return { default: Hls }
})

function media(canPlay = ''): HTMLMediaElement {
    const element = document.createElement('video')
    element.canPlayType = vi.fn(() => canPlay as CanPlayTypeResult)
    return element
}

describe('browserHlsSupport', () => {
    beforeEach(() => {
        fake.state.supported = true
        fake.state.instances.length = 0
    })

    it('plays through hls.js where Media Source Extensions exist', async () => {
        const element = media()
        const onError = vi.fn()

        const attachment = await browserHlsSupport.attach(element, '/master.m3u8', onError)

        const hls = fake.state.instances[0]!
        expect(hls.loadSource).toHaveBeenCalledWith('/master.m3u8')
        expect(hls.attachMedia).toHaveBeenCalledWith(element)
        attachment.destroy()
        expect(hls.destroy).toHaveBeenCalledOnce()
    })

    it('reports fatal errors only', async () => {
        const onError = vi.fn()
        await browserHlsSupport.attach(media(), '/master.m3u8', onError)
        const handler = fake.state.instances[0]!.handlers.get('hlsError')!

        handler('hlsError', { fatal: false, details: 'fragLoadError' })
        expect(onError).not.toHaveBeenCalled()
        handler('hlsError', { fatal: true, details: 'manifestLoadError' })
        expect(onError).toHaveBeenCalledOnce()
    })

    it('falls back to native HLS', async () => {
        fake.state.supported = false
        const element = media('maybe')

        const attachment = await browserHlsSupport.attach(element, '/master.m3u8', vi.fn())

        expect(element.canPlayType).toHaveBeenCalledWith(hlsContentType)
        expect(element.getAttribute('src')).toBe('/master.m3u8')
        attachment.destroy()
        expect(element.getAttribute('src')).toBeNull()
    })

    it('fails without any HLS support', async () => {
        fake.state.supported = false

        await expect(browserHlsSupport.attach(media(), '/master.m3u8', vi.fn())).rejects.toThrow()
    })
})
