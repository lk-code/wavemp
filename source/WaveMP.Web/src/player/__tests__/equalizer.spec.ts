import { describe, expect, it } from 'vitest'
import type { Equalizer } from '@/api/generated/models'
import {
    clampGain,
    dbToGain,
    defaultEqualizerSettings,
    effectiveGains,
    equalizerOutput,
    equalizerPresets,
    fallbackEqualizerBands,
    resetEqualizer,
    toEqualizerDefinition,
    toEqualizerSettings,
    withBand,
    withPreamp,
    withPreset,
    type EqualizerSettings,
} from '../equalizer'

const rock = [5, 4, 3, 1, -1, -1, 1, 3, 4, 5]

const answer: Equalizer = {
    minGainDb: -12,
    maxGainDb: 12,
    bands: fallbackEqualizerBands.map((band) => ({
        frequencyHz: band.frequency,
        filterType: band.type,
        q: band.q,
    })),
    presets: [
        { preset: 'Flat', gainsDb: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
        { preset: 'Rock', gainsDb: rock },
    ],
}

const definition = toEqualizerDefinition(answer)!

const own = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

function settings(change: Partial<EqualizerSettings> = {}): EqualizerSettings {
    return { ...defaultEqualizerSettings, customGainsDb: own, ...change }
}

describe('equalizer', () => {
    it('turns decibels into gain factors', () => {
        expect(dbToGain(0)).toBe(1)
        expect(dbToGain(20)).toBeCloseTo(10)
        expect(dbToGain(-6)).toBeCloseTo(0.501, 3)
    })

    it('knows the ten octave bands of the server for the start', () => {
        expect(fallbackEqualizerBands.map((band) => band.frequency)).toEqual([
            31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000,
        ])
        expect(fallbackEqualizerBands[0]!.type).toBe('LowShelf')
        expect(fallbackEqualizerBands[9]!.type).toBe('HighShelf')
    })

    it('lists every preset with Custom last', () => {
        expect(equalizerPresets[0]).toBe('Flat')
        expect(equalizerPresets.at(-1)).toBe('Custom')
        expect(equalizerPresets).toHaveLength(10)
    })

    it('reads the definition of the server', () => {
        expect(definition.bands).toEqual(fallbackEqualizerBands)
        expect(definition.presets.get('Rock')).toEqual(rock)
        expect(toEqualizerDefinition({ ...answer, bands: answer.bands!.slice(1) })).toBeNull()
        expect(toEqualizerDefinition(undefined)).toBeNull()
    })

    it('reads complete settings only', () => {
        const value = {
            enabled: true,
            preset: 'Rock',
            preampDb: -2,
            gainsDb: rock,
            customGainsDb: own,
        }

        expect(toEqualizerSettings(value)).toEqual(value)
        expect(toEqualizerSettings({ ...value, preset: 'Loudness' })).toBeNull()
        expect(toEqualizerSettings({ ...value, customGainsDb: [1] })).toBeNull()
        expect(toEqualizerSettings(null)).toBeNull()
    })

    it('applies the curve of the preset, or the own curve for Custom', () => {
        expect(effectiveGains(settings({ preset: 'Rock' }), definition)).toEqual(rock)
        expect(effectiveGains(settings({ preset: 'Custom' }), definition)).toEqual(own)
    })

    it('applies the curve of the server while the definition is unknown', () => {
        const value = settings({ preset: 'Jazz', gainsDb: [4, 3, 1, 2, -2, -2, 0, 1, 3, 4] })

        expect(effectiveGains(value, null)).toEqual(value.gainsDb)
        expect(equalizerOutput({ ...value, enabled: true, preampDb: -1 }, null)).toEqual({
            enabled: true,
            preampDb: -1,
            gainsDb: value.gainsDb,
        })
    })

    it('turns a preset into the own curve when a band moves', () => {
        const result = withBand(settings({ preset: 'Rock' }), 5, 3.2, definition)

        expect(result.preset).toBe('Custom')
        expect(result.customGainsDb).toEqual([5, 4, 3, 1, -1, 3, 1, 3, 4, 5])
        expect(result.gainsDb).toEqual(result.customGainsDb)
    })

    it('keeps the own curve when a preset is selected', () => {
        const result = withPreset(settings({ preset: 'Custom' }), 'Pop')

        expect(result.preset).toBe('Pop')
        expect(withPreset(result, 'Custom').customGainsDb).toEqual(own)
    })

    it('limits gains to the range and the step of the sliders', () => {
        expect(clampGain(13)).toBe(12)
        expect(clampGain(-20, definition)).toBe(-12)
        expect(clampGain(1.3)).toBe(1.5)
        expect(clampGain(-0.2)).toBe(0)
        expect(clampGain(Number.NaN)).toBe(0)
        expect(withPreamp(settings(), 30, definition).preampDb).toBe(12)
    })

    it('resets to Flat without a preamp and keeps the own curve', () => {
        const result = resetEqualizer(settings({ preset: 'Custom', preampDb: -4, enabled: true }))

        expect(result).toMatchObject({
            preset: 'Flat',
            preampDb: 0,
            enabled: true,
            customGainsDb: own,
        })
        expect(result.gainsDb.every((gain) => gain === 0)).toBe(true)
    })
})
