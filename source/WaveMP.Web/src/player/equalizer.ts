// Logic of the equalizer, independent of Web Audio: the curves of the presets come from the server
// (GET /api/v1/equalizer), the settings of the profile from GET /api/v1/me/settings. Pure functions.

import {
    EqualizerFilterTypeObject,
    EqualizerPresetObject,
    type Equalizer,
    type EqualizerFilterType,
    type EqualizerPreset,
} from '@/api/generated/models'

/** Number of bands of the equalizer. */
export const equalizerBandCount = 10

/** Lowest and highest gain of a band and of the preamp in dB, as long as the definition is unknown. */
export const fallbackMinGainDb = -12
export const fallbackMaxGainDb = 12

/** Step of the sliders in dB. */
export const equalizerStepDb = 0.5

/** What the audio engine applies: on or off, preamp and the curve in dB. */
export interface EqualizerState {
    readonly enabled: boolean
    readonly preampDb: number
    readonly gainsDb: readonly number[]
}

/** A band as Web Audio builds it. */
export interface EqualizerBandLayout {
    readonly frequency: number
    readonly type: EqualizerFilterType
    readonly q: number
}

/** The settings of the equalizer of a profile. */
export interface EqualizerSettings {
    readonly enabled: boolean
    readonly preset: EqualizerPreset
    readonly preampDb: number
    /** The curve that applies, as the server returned it. */
    readonly gainsDb: readonly number[]
    /** The own curve of the profile. */
    readonly customGainsDb: readonly number[]
}

/** The definition of the equalizer the server describes. */
export interface EqualizerDefinition {
    readonly minGainDb: number
    readonly maxGainDb: number
    readonly bands: readonly EqualizerBandLayout[]
    /** Curve per preset; Custom is the own curve of the profile and not part of it. */
    readonly presets: ReadonlyMap<EqualizerPreset, readonly number[]>
}

/**
 * The bands as the server describes them (31 Hz to 16 kHz in octaves), for the start before the
 * definition is loaded, e.g. offline.
 */
export const fallbackEqualizerBands: readonly EqualizerBandLayout[] = [
    31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000,
].map((frequency, index, all) => ({
    frequency,
    type:
        index === 0
            ? EqualizerFilterTypeObject.LowShelf
            : index === all.length - 1
              ? EqualizerFilterTypeObject.HighShelf
              : EqualizerFilterTypeObject.Peaking,
    q: index === 0 || index === all.length - 1 ? 0.71 : 1.41,
}))

/** All presets in the order of the API, Custom last. */
export const equalizerPresets: readonly EqualizerPreset[] = Object.values(EqualizerPresetObject)

const presetNames = new Set<string>(equalizerPresets)
const filterTypes = new Set<string>(Object.values(EqualizerFilterTypeObject))

export function flatCurve(): number[] {
    return Array.from({ length: equalizerBandCount }, () => 0)
}

export const defaultEqualizerSettings: EqualizerSettings = {
    enabled: false,
    preset: EqualizerPresetObject.Flat,
    preampDb: 0,
    gainsDb: flatCurve(),
    customGainsDb: flatCurve(),
}

export function isEqualizerPreset(value: unknown): value is EqualizerPreset {
    return typeof value === 'string' && presetNames.has(value)
}

function isCurve(value: unknown): value is number[] {
    return (
        Array.isArray(value) &&
        value.length === equalizerBandCount &&
        value.every((gain) => typeof gain === 'number' && Number.isFinite(gain))
    )
}

/** The settings of the equalizer from an answer of the server, or null if they are incomplete. */
export function toEqualizerSettings(value: unknown): EqualizerSettings | null {
    const settings = value as Partial<Record<keyof EqualizerSettings, unknown>> | null | undefined
    if (
        !settings ||
        typeof settings.enabled !== 'boolean' ||
        !isEqualizerPreset(settings.preset) ||
        typeof settings.preampDb !== 'number' ||
        !Number.isFinite(settings.preampDb) ||
        !isCurve(settings.gainsDb) ||
        !isCurve(settings.customGainsDb)
    ) {
        return null
    }

    return {
        enabled: settings.enabled,
        preset: settings.preset,
        preampDb: settings.preampDb,
        gainsDb: [...settings.gainsDb],
        customGainsDb: [...settings.customGainsDb],
    }
}

/** The definition from the answer of the server, or null if it is incomplete. */
export function toEqualizerDefinition(
    value: Equalizer | null | undefined,
): EqualizerDefinition | null {
    const bands = (value?.bands ?? []).flatMap((band): EqualizerBandLayout[] =>
        typeof band.frequencyHz === 'number' &&
        typeof band.q === 'number' &&
        band.filterType &&
        filterTypes.has(band.filterType)
            ? [{ frequency: band.frequencyHz, type: band.filterType, q: band.q }]
            : [],
    )
    if (
        !value ||
        bands.length !== equalizerBandCount ||
        typeof value.minGainDb !== 'number' ||
        typeof value.maxGainDb !== 'number'
    ) {
        return null
    }

    const presets = new Map<EqualizerPreset, readonly number[]>()
    for (const curve of value.presets ?? []) {
        if (isEqualizerPreset(curve.preset) && isCurve(curve.gainsDb)) {
            presets.set(curve.preset, [...curve.gainsDb])
        }
    }

    return { minGainDb: value.minGainDb, maxGainDb: value.maxGainDb, bands, presets }
}

/** Gain factor of a level in dB. */
export function dbToGain(db: number): number {
    return Math.pow(10, db / 20)
}

/**
 * The curve that applies: the own curve for Custom, otherwise the curve of the preset from the
 * definition; without a definition the curve the server returned with the settings.
 */
export function effectiveGains(
    settings: EqualizerSettings,
    definition: EqualizerDefinition | null,
): readonly number[] {
    if (settings.preset === EqualizerPresetObject.Custom) {
        return settings.customGainsDb
    }

    return definition?.presets.get(settings.preset) ?? settings.gainsDb
}

/** What the engine applies for the settings. */
export function equalizerOutput(
    settings: EqualizerSettings,
    definition: EqualizerDefinition | null,
): EqualizerState {
    return {
        enabled: settings.enabled,
        preampDb: settings.preampDb,
        gainsDb: effectiveGains(settings, definition),
    }
}

/** A gain within the limits, rounded to the step of the sliders. */
export function clampGain(db: number, definition: EqualizerDefinition | null = null): number {
    const min = definition?.minGainDb ?? fallbackMinGainDb
    const max = definition?.maxGainDb ?? fallbackMaxGainDb
    if (!Number.isFinite(db)) {
        return 0
    }

    const rounded = Math.round(db / equalizerStepDb) * equalizerStepDb
    return Math.min(max, Math.max(min, rounded)) || 0
}

/** Selects a preset; the own curve stays. */
export function withPreset(
    settings: EqualizerSettings,
    preset: EqualizerPreset,
): EqualizerSettings {
    return { ...settings, preset }
}

/**
 * Changes one band. Like common equalizers, moving a band of a preset turns the curve into the own
 * curve: the curve that applied, with the change, and selects Custom.
 */
export function withBand(
    settings: EqualizerSettings,
    index: number,
    db: number,
    definition: EqualizerDefinition | null,
): EqualizerSettings {
    if (index < 0 || index >= equalizerBandCount) {
        return settings
    }

    const curve = [...effectiveGains(settings, definition)]
    curve[index] = clampGain(db, definition)
    return {
        ...settings,
        preset: EqualizerPresetObject.Custom,
        gainsDb: curve,
        customGainsDb: curve,
    }
}

export function withPreamp(
    settings: EqualizerSettings,
    db: number,
    definition: EqualizerDefinition | null,
): EqualizerSettings {
    return { ...settings, preampDb: clampGain(db, definition) }
}

/** Back to neutral: preset Flat and no preamp; the own curve stays for later. */
export function resetEqualizer(settings: EqualizerSettings): EqualizerSettings {
    return {
        ...settings,
        preset: EqualizerPresetObject.Flat,
        preampDb: 0,
        gainsDb: flatCurve(),
    }
}
