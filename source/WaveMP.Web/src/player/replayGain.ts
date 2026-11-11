// ReplayGain: evens out the volume of tracks with the gains of their tags. Pure functions.

import { ReplayGainModeObject, type ReplayGain, type ReplayGainMode } from '@/api/generated/models'

/** Highest gain factor: louder than +15 dB is a broken tag rather than a quiet track. */
export const maxReplayGainFactor = 10 ** (15 / 20)

function finite(value: number | null | undefined): number | undefined {
    return typeof value === 'number' && Number.isFinite(value) ? value : undefined
}

/**
 * The factor a track is played with: 1 when off or without tags. Track mode prefers the gain of the
 * track, album mode that of the album; each falls back to the other. The peak of the same tag
 * limits the factor, so the track does not clip.
 */
export function replayGainFactor(
    mode: ReplayGainMode,
    values: ReplayGain | null | undefined,
): number {
    if (mode === ReplayGainModeObject.Off || !values) {
        return 1
    }

    const track = { gain: finite(values.trackGain), peak: finite(values.trackPeak) }
    const album = { gain: finite(values.albumGain), peak: finite(values.albumPeak) }
    const [first, second] = mode === ReplayGainModeObject.Album ? [album, track] : [track, album]
    const chosen = first.gain !== undefined ? first : second
    if (chosen.gain === undefined) {
        return 1
    }

    let factor = Math.min(10 ** (chosen.gain / 20), maxReplayGainFactor)
    if (chosen.peak !== undefined && chosen.peak > 0) {
        factor = Math.min(factor, 1 / chosen.peak)
    }

    return factor
}
