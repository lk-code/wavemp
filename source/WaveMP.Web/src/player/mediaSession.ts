// Media Session API: shows the current track on the lock screen, in notifications and on media
// keys, and passes their buttons to the player. Browsers without the API are left alone.

export interface MediaSessionHandlers {
    play(): void
    pause(): void
    stop(): void
    previous(): void
    next(): void
    seekTo(position: number): void
}

export interface MediaSessionState {
    readonly playback: 'idle' | 'playing' | 'paused'
    readonly title?: string
    readonly artist?: string
    readonly album?: string
    /** URL of the cover (an object URL), if loaded. */
    readonly artwork?: string | null
    /** Position and duration in seconds. */
    readonly position: number
    readonly duration: number
}

/** The metadata last shown per session, to set it only when it changes. */
const shown = new WeakMap<MediaSession, string>()

function defaultSession(): MediaSession | null {
    return typeof navigator !== 'undefined' && 'mediaSession' in navigator
        ? navigator.mediaSession
        : null
}

/** Registers the buttons; returns the function that removes them again. */
export function connectMediaSession(
    handlers: MediaSessionHandlers,
    session: MediaSession | null = defaultSession(),
): () => void {
    if (!session) {
        return () => undefined
    }

    const actions: [MediaSessionAction, MediaSessionActionHandler][] = [
        ['play', () => handlers.play()],
        ['pause', () => handlers.pause()],
        ['stop', () => handlers.stop()],
        ['previoustrack', () => handlers.previous()],
        ['nexttrack', () => handlers.next()],
        [
            'seekto',
            (details) => {
                if (typeof details.seekTime === 'number') {
                    handlers.seekTo(details.seekTime)
                }
            },
        ],
    ]
    const registered: MediaSessionAction[] = []
    for (const [action, handler] of actions) {
        try {
            session.setActionHandler(action, handler)
            registered.push(action)
        } catch {
            // The browser does not know this action.
        }
    }

    return () => {
        for (const action of registered) {
            try {
                session.setActionHandler(action, null)
            } catch {
                // Nothing to remove.
            }
        }
    }
}

/** Shows the state of the player in the media session. */
export function updateMediaSession(
    state: MediaSessionState,
    session: MediaSession | null = defaultSession(),
): void {
    if (!session) {
        return
    }

    session.playbackState = state.playback === 'idle' ? 'none' : state.playback
    if (state.playback === 'idle' || !state.title) {
        session.metadata = null
        shown.delete(session)
        return
    }

    const key = JSON.stringify([state.title, state.artist, state.album, state.artwork])
    if (typeof MediaMetadata !== 'undefined' && shown.get(session) !== key) {
        session.metadata = new MediaMetadata({
            title: state.title,
            artist: state.artist ?? '',
            album: state.album ?? '',
            artwork: state.artwork ? [{ src: state.artwork }] : [],
        })
        shown.set(session, key)
    }

    if (state.duration > 0 && typeof session.setPositionState === 'function') {
        try {
            session.setPositionState({
                duration: state.duration,
                position: Math.min(Math.max(state.position, 0), state.duration),
                playbackRate: 1,
            })
        } catch {
            // Invalid values in between two tracks; the next update corrects them.
        }
    }
}
