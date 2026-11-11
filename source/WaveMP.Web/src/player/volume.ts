// Volume of the player. The slider is linear, the ear is not: the gain follows the square of the
// slider, so the lower half of the slider stays usable. Pure functions besides the stored values.

export interface VolumeSettings {
    /** Position of the volume slider between 0 and 1. */
    readonly volume: number
    readonly muted: boolean
}

export const defaultVolume: VolumeSettings = { volume: 0.8, muted: false }

/** Key in localStorage: the volume belongs to the device, not to the profile. */
export const volumeStorageKey = 'wavemp.player.volume.v1'

export function clampVolume(volume: number): number {
    return Number.isFinite(volume) ? Math.min(1, Math.max(0, volume)) : defaultVolume.volume
}

/** The gain of the output for the slider position; 0 while muted. */
export function volumeToGain(settings: VolumeSettings): number {
    return settings.muted ? 0 : clampVolume(settings.volume) ** 2
}

function storage(): Storage | null {
    try {
        return window.localStorage
    } catch {
        return null
    }
}

/** The stored volume of this device, or the default. */
export function loadVolume(store: Storage | null = storage()): VolumeSettings {
    try {
        const value: unknown = JSON.parse(store?.getItem(volumeStorageKey) ?? 'null')
        if (typeof value === 'object' && value !== null) {
            const { volume, muted } = value as Record<string, unknown>
            if (typeof volume === 'number' && typeof muted === 'boolean') {
                return { volume: clampVolume(volume), muted }
            }
        }
    } catch {
        // Invalid or blocked storage: the default applies.
    }

    return defaultVolume
}

export function saveVolume(settings: VolumeSettings, store: Storage | null = storage()): void {
    try {
        store?.setItem(volumeStorageKey, JSON.stringify(settings))
    } catch {
        // Blocked storage: the volume applies until the page is closed.
    }
}
