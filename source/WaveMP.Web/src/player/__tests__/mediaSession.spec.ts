import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { connectMediaSession, updateMediaSession, type MediaSessionHandlers } from '../mediaSession'

class FakeMetadata {
    title: string
    artist: string
    album: string
    artwork: { src: string }[]

    constructor(init: {
        title: string
        artist: string
        album: string
        artwork: { src: string }[]
    }) {
        this.title = init.title
        this.artist = init.artist
        this.album = init.album
        this.artwork = init.artwork
    }
}

function fakeSession() {
    const handlers = new Map<string, MediaSessionActionHandler | null>()
    return {
        handlers,
        metadata: null as FakeMetadata | null,
        playbackState: 'none' as MediaSessionPlaybackState,
        setActionHandler: vi.fn((action: string, handler: MediaSessionActionHandler | null) => {
            if (action === 'stop') {
                throw new TypeError('unsupported')
            }
            handlers.set(action, handler)
        }),
        setPositionState: vi.fn(),
    }
}

describe('media session', () => {
    beforeEach(() => {
        vi.stubGlobal('MediaMetadata', FakeMetadata)
    })

    afterEach(() => {
        vi.unstubAllGlobals()
    })

    it('passes the buttons to the player and removes them again', () => {
        const session = fakeSession()
        const handlers: MediaSessionHandlers = {
            play: vi.fn(),
            pause: vi.fn(),
            stop: vi.fn(),
            previous: vi.fn(),
            next: vi.fn(),
            seekTo: vi.fn(),
        }

        const disconnect = connectMediaSession(handlers, session as unknown as MediaSession)
        session.handlers.get('play')?.({ action: 'play' })
        session.handlers.get('nexttrack')?.({ action: 'nexttrack' })
        session.handlers.get('seekto')?.({ action: 'seekto', seekTime: 12 })
        disconnect()

        expect(handlers.play).toHaveBeenCalledOnce()
        expect(handlers.next).toHaveBeenCalledOnce()
        expect(handlers.seekTo).toHaveBeenCalledWith(12)
        expect([...session.handlers.values()].every((handler) => handler === null)).toBe(true)
    })

    it('does nothing without the API', () => {
        expect(() => connectMediaSession({} as MediaSessionHandlers, null)()).not.toThrow()
        expect(() =>
            updateMediaSession({ playback: 'idle', position: 0, duration: 0 }, null),
        ).not.toThrow()
    })

    it('shows the track, its state and position', () => {
        const session = fakeSession()

        updateMediaSession(
            {
                playback: 'playing',
                title: 'Song',
                artist: 'Band',
                album: 'Album',
                artwork: 'blob:cover',
                position: 12,
                duration: 10,
            },
            session as unknown as MediaSession,
        )

        expect(session.playbackState).toBe('playing')
        expect(session.metadata).toMatchObject({
            title: 'Song',
            artist: 'Band',
            album: 'Album',
            artwork: [{ src: 'blob:cover' }],
        })
        expect(session.setPositionState).toHaveBeenCalledWith({
            duration: 10,
            position: 10,
            playbackRate: 1,
        })
    })

    it('keeps the metadata while the track stays and clears it when idle', () => {
        const session = fakeSession()
        const state = { playback: 'paused', title: 'Song', position: 1, duration: 10 } as const

        updateMediaSession(state, session as unknown as MediaSession)
        const first = session.metadata
        updateMediaSession({ ...state, position: 2 }, session as unknown as MediaSession)
        expect(session.metadata).toBe(first)

        updateMediaSession(
            { playback: 'idle', position: 0, duration: 0 },
            session as unknown as MediaSession,
        )
        expect(session.metadata).toBeNull()
        expect(session.playbackState).toBe('none')
    })
})
