// State of the transport bar. The bar is controlled through props only; the player store
// (src/stores/player.ts) provides the state and handles the events.

import type { IconSlotName } from '@/themes/iconSlots'

export type PlaybackState = 'idle' | 'playing' | 'paused'

export type RepeatMode = 'off' | 'all' | 'one'

/**
 * Notice of the player: the track needs transcoding (not available yet), could not be loaded or
 * failed to play. The player skips to the next track.
 */
export type PlayerMessage = 'unsupported' | 'unavailable' | 'failed'

export interface TransportState {
    playback: PlaybackState
    /** The track is being loaded or waits for data. */
    loading?: boolean
    /** Id of the current track; without one, there is nothing to play. */
    trackId?: string
    /** Whether the current track has a cover. */
    hasCover?: boolean
    title?: string
    artist?: string
    /** Favorite and rating (0 to 5) of the current track, as the API returned them. */
    isFavorite?: boolean
    rating?: number
    message?: PlayerMessage | null
    /** Position and duration in seconds. */
    position: number
    duration: number
    shuffle: boolean
    repeat: RepeatMode
    /** Volume between 0 and 1. */
    volume: number
    muted: boolean
}

/** Repeat cycles through off, all and one. */
export function nextRepeatMode(mode: RepeatMode): RepeatMode {
    switch (mode) {
        case 'off':
            return 'all'
        case 'all':
            return 'one'
        default:
            return 'off'
    }
}

export interface TransportIcons {
    readonly play: Extract<IconSlotName, 'play' | 'pause'>
    readonly repeat: Extract<IconSlotName, 'repeat' | 'repeat-one'>
    readonly volume: Extract<IconSlotName, 'volume' | 'muted'>
}

/** Icon slots that show the state of the play, repeat and mute buttons. */
export function transportIcons(
    state: Pick<TransportState, 'playback' | 'repeat' | 'muted'>,
): TransportIcons {
    return {
        play: state.playback === 'playing' ? 'pause' : 'play',
        repeat: state.repeat === 'one' ? 'repeat-one' : 'repeat',
        volume: state.muted ? 'muted' : 'volume',
    }
}

/** data-state tokens of the transport bar, e.g. "playing loading expanded". */
export function transportStateTokens(
    playback: PlaybackState,
    expanded: boolean,
    loading = false,
): string {
    return [playback, ...(loading ? ['loading'] : []), ...(expanded ? ['expanded'] : [])].join(' ')
}
