// Timing rules of the player. Pure functions.

/** A play counts for the history after half the track or four minutes, whichever comes first. */
export const historyMaxThreshold = 240

/** Before the end of a track, the next one is prepared, so it can start without a gap. */
export const preloadLead = 20

/** "Previous" restarts the current track once it has played longer than this. */
export const restartThreshold = 3

/** Seconds a track has to be listened to for the history; duration in seconds, 0 if unknown. */
export function historyThreshold(duration: number): number {
    if (!Number.isFinite(duration) || duration <= 0) {
        return historyMaxThreshold
    }

    return Math.min(duration / 2, historyMaxThreshold)
}

/** Whether the next track should be prepared now. */
export function shouldPreload(position: number, duration: number): boolean {
    return Number.isFinite(duration) && duration > 0 && duration - position <= preloadLead
}

/** Whether "previous" restarts the current track instead of going back. */
export function shouldRestart(position: number): boolean {
    return position > restartThreshold
}

/**
 * Listened time between two positions: only small forward steps of the playback count, seeking
 * does not.
 */
export function listenedBetween(previous: number, current: number): number {
    const step = current - previous
    return step > 0 && step < 2 ? step : 0
}
