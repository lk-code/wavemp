// How the muted video of the Now Playing stage follows the audio, which always leads. The video
// should show the position of the audio plus the offset of the track; small differences are made
// up by playing the video a little faster or slower, larger ones by seeking. Pure functions, so
// the rules are tested without a browser.

import { PlayerViewModeObject, type PlayerViewMode } from '@/api/generated/models'

/** What the stage shows. */
export type StageView = 'cover' | 'video' | 'visualization'

/** State of the video of the current track: not loaded, loading or positioning, ready, failed. */
export type VideoState = 'idle' | 'loading' | 'ready' | 'failed'

/** Seconds of difference from which the video seeks instead of changing its speed. */
export const hardSyncThreshold = 0.3

/** Seconds of difference below which the video is in sync. */
export const softSyncThreshold = 0.04

/** Largest change of the speed of the video, as a share of the normal speed. */
export const maxRateCorrection = 0.08

/** Seconds in which a change of the speed should make up a difference. */
export const rateCorrectionWindow = 2

/** Seconds between two seeks, so a slow stream (HLS) does not end up seeking all the time. */
export const minSeekInterval = 1

/** Seconds before the end of the video at which it stops on its last picture. */
export const endMargin = 0.05

export interface SyncInput {
    /** Position of the audio in seconds. */
    readonly audioPosition: number
    /** Whether the audio plays. */
    readonly playing: boolean
    /** Position of the video in seconds. */
    readonly videoPosition: number
    /** Offset of the video against the audio in seconds. */
    readonly offset: number
    /** Duration of the video in seconds; 0 while unknown. */
    readonly videoDuration: number
    /** Seconds since the last seek of the video. */
    readonly sinceLastSeek: number
}

export type SyncAction =
    /** The video waits paused at this position: it starts later or already ended. */
    | { readonly kind: 'hold'; readonly at: number }
    /** The video jumps to this position. */
    | { readonly kind: 'seek'; readonly to: number }
    /** The video plays (or waits) at this speed; 1 is the normal speed. */
    | { readonly kind: 'rate'; readonly rate: number }

/** The position of the video that belongs to a position of the audio. */
export function videoTarget(audioPosition: number, offset: number): number {
    return audioPosition + offset
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value))
}

/** What the video does to show the position of the audio. */
export function syncAction(input: SyncInput): SyncAction {
    const target = videoTarget(input.audioPosition, input.offset)
    if (target < 0) {
        return { kind: 'hold', at: 0 }
    }

    if (input.videoDuration > 0 && target >= input.videoDuration - endMargin) {
        return { kind: 'hold', at: Math.max(0, input.videoDuration - endMargin) }
    }

    // Positive: the video is ahead of the audio.
    const drift = input.videoPosition - target
    const distance = Math.abs(drift)
    if (distance <= softSyncThreshold) {
        return { kind: 'rate', rate: 1 }
    }

    // A paused video cannot catch up by its speed.
    const mayJump = input.sinceLastSeek >= minSeekInterval
    if (mayJump && (distance > hardSyncThreshold || !input.playing)) {
        return { kind: 'seek', to: target }
    }

    const rate = clamp(
        1 - drift / rateCorrectionWindow,
        1 - maxRateCorrection,
        1 + maxRateCorrection,
    )
    return { kind: 'rate', rate }
}

/**
 * The view the stage shows for the chosen mode: the video only when the track has one and it is
 * ready at the right position, otherwise the cover.
 */
export function stageViewOf(mode: PlayerViewMode, hasVideo: boolean, video: VideoState): StageView {
    if (mode === PlayerViewModeObject.Visualization) {
        return 'visualization'
    }

    return mode === PlayerViewModeObject.Video && hasVideo && video === 'ready' ? 'video' : 'cover'
}
