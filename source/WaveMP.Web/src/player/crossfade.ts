// Rules of the crossfade between tracks. Pure functions.

import type { Track } from '@/api/generated/models'

/** Points of the fade curves of a crossfade. */
export const crossfadeCurveSteps = 64

/** Longest crossfade in seconds, as the API allows it. */
export const maxCrossfadeSeconds = 12

/** The crossfade settings of a profile. */
export interface CrossfadeSettings {
    /** Seconds of the crossfade; 0 plays the tracks gapless. */
    readonly seconds: number
    /** Whether consecutive tracks of the same album play gapless. */
    readonly albumsGapless: boolean
}

export const defaultCrossfadeSettings: CrossfadeSettings = { seconds: 0, albumsGapless: true }

/** The crossfade settings from an answer of the server, or null if they are incomplete. */
export function toCrossfadeSettings(value: unknown): CrossfadeSettings | null {
    const settings = value as Partial<Record<keyof CrossfadeSettings, unknown>> | null | undefined
    if (
        !settings ||
        typeof settings.seconds !== 'number' ||
        !Number.isInteger(settings.seconds) ||
        settings.seconds < 0 ||
        settings.seconds > maxCrossfadeSeconds ||
        typeof settings.albumsGapless !== 'boolean'
    ) {
        return null
    }

    return { seconds: settings.seconds, albumsGapless: settings.albumsGapless }
}

function durationOf(track: Pick<Track, 'durationMilliseconds'>, known?: number): number {
    if (known !== undefined && known > 0) {
        return known
    }

    const milliseconds = track.durationMilliseconds
    return typeof milliseconds === 'number' && milliseconds > 0 ? milliseconds / 1000 : 0
}

function sameAlbum(current: Pick<Track, 'album'>, next: Pick<Track, 'album'>): boolean {
    const album = current.album?.id
    return !!album && album === next.album?.id
}

/**
 * Seconds the current track fades into the next when it ends on its own: 0 if the crossfade is off
 * or both belong to the same album and albums play gapless. A crossfade never takes more than half
 * of either track; an unknown duration does not limit it. Manual skips never fade.
 */
export function crossfadeDuration(
    settings: CrossfadeSettings,
    current: Pick<Track, 'album' | 'durationMilliseconds'>,
    next: Pick<Track, 'album' | 'durationMilliseconds'>,
    currentDuration?: number,
): number {
    if (settings.seconds <= 0 || (settings.albumsGapless && sameAlbum(current, next))) {
        return 0
    }

    const limits = [durationOf(current, currentDuration), durationOf(next)]
        .filter((duration) => duration > 0)
        .map((duration) => duration / 2)
    return Math.max(0, Math.min(settings.seconds, maxCrossfadeSeconds, ...limits))
}

/**
 * An equal-power curve from 0 to 1 (in) or 1 to 0 (out): sin and cos of a quarter turn, so the sum of
 * the squares of both curves stays 1 and the loudness does not dip in the middle.
 */
export function equalPowerCurve(
    direction: 'in' | 'out',
    steps = crossfadeCurveSteps,
): Float32Array {
    const count = Math.max(2, Math.floor(steps))
    const curve = new Float32Array(count)
    for (let index = 0; index < count; index++) {
        const angle = (index / (count - 1)) * (Math.PI / 2)
        curve[index] = direction === 'in' ? Math.sin(angle) : Math.cos(angle)
    }

    return curve
}
