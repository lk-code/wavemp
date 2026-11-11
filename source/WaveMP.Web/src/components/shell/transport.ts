// State of the transport bar. The bar is controlled through props only, so the source of the
// state can be exchanged: a placeholder until step 9, then the player.

import type { IconSlotName } from '@/themes/iconSlots'

export type PlaybackState = 'idle' | 'playing' | 'paused'

export type RepeatMode = 'off' | 'all' | 'one'

export interface TransportState {
    playback: PlaybackState
    title?: string
    artist?: string
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

/** data-state tokens of the transport bar, e.g. "playing expanded". */
export function transportStateTokens(playback: PlaybackState, expanded: boolean): string {
    return expanded ? `${playback} expanded` : playback
}
