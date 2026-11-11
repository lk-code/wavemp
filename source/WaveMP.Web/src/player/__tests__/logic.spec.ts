import { describe, expect, it } from 'vitest'
import { AudioFormatObject, ReplayGainModeObject } from '@/api/generated/models'
import { audioFormatProbes, supportedAudioFormats } from '../formats'
import { maxReplayGainFactor, replayGainFactor } from '../replayGain'
import {
    historyMaxThreshold,
    historyThreshold,
    listenedBetween,
    shouldPreload,
    shouldRestart,
} from '../scheduling'
import {
    clampVolume,
    defaultVolume,
    loadVolume,
    saveVolume,
    volumeStorageKey,
    volumeToGain,
} from '../volume'

describe('formats', () => {
    it('has a probe for every format of the API', () => {
        expect(Object.keys(audioFormatProbes).sort()).toEqual(
            Object.values(AudioFormatObject).sort(),
        )
    })

    it('lists the formats the browser can play, "maybe" included', () => {
        const answers: Record<string, string> = {
            'audio/mpeg': 'maybe',
            'audio/flac': 'probably',
            'audio/ogg; codecs="opus"': 'probably',
        }

        expect(supportedAudioFormats((type) => answers[type] ?? '')).toEqual([
            AudioFormatObject.Mp3,
            AudioFormatObject.Flac,
            AudioFormatObject.OggOpus,
        ])
    })
})

describe('replayGainFactor', () => {
    const values = { trackGain: -6, trackPeak: 0.5, albumGain: -12, albumPeak: 0.9 }

    it('leaves the volume when off or without tags', () => {
        expect(replayGainFactor(ReplayGainModeObject.Off, values)).toBe(1)
        expect(replayGainFactor(ReplayGainModeObject.Track, null)).toBe(1)
        expect(replayGainFactor(ReplayGainModeObject.Track, {})).toBe(1)
    })

    it('uses the gain of the track or of the album', () => {
        expect(replayGainFactor(ReplayGainModeObject.Track, values)).toBeCloseTo(10 ** (-6 / 20))
        expect(replayGainFactor(ReplayGainModeObject.Album, values)).toBeCloseTo(10 ** (-12 / 20))
    })

    it('falls back to the other gain', () => {
        expect(replayGainFactor(ReplayGainModeObject.Album, { trackGain: -3 })).toBeCloseTo(
            10 ** (-3 / 20),
        )
        expect(replayGainFactor(ReplayGainModeObject.Track, { albumGain: -9 })).toBeCloseTo(
            10 ** (-9 / 20),
        )
    })

    it('keeps a positive gain below clipping and within the limit', () => {
        expect(replayGainFactor(ReplayGainModeObject.Track, { trackGain: 6, trackPeak: 0.8 })).toBe(
            1 / 0.8,
        )
        expect(replayGainFactor(ReplayGainModeObject.Track, { trackGain: 40 })).toBe(
            maxReplayGainFactor,
        )
    })
})

describe('volume', () => {
    it('follows the square of the slider and is silent while muted', () => {
        expect(volumeToGain({ volume: 0.5, muted: false })).toBe(0.25)
        expect(volumeToGain({ volume: 1, muted: false })).toBe(1)
        expect(volumeToGain({ volume: 1, muted: true })).toBe(0)
    })

    it('clamps the slider', () => {
        expect(clampVolume(2)).toBe(1)
        expect(clampVolume(-1)).toBe(0)
        expect(clampVolume(Number.NaN)).toBe(defaultVolume.volume)
    })

    it('stores the volume of the device', () => {
        const values = new Map<string, string>()
        const storage = {
            getItem: (key: string) => values.get(key) ?? null,
            setItem: (key: string, value: string) => values.set(key, value),
        } as unknown as Storage

        expect(loadVolume(storage)).toEqual(defaultVolume)
        saveVolume({ volume: 0.3, muted: true }, storage)
        expect(loadVolume(storage)).toEqual({ volume: 0.3, muted: true })

        values.set(volumeStorageKey, '{"volume":"loud"}')
        expect(loadVolume(storage)).toEqual(defaultVolume)
        values.set(volumeStorageKey, 'not json')
        expect(loadVolume(storage)).toEqual(defaultVolume)
        expect(loadVolume(null)).toEqual(defaultVolume)
    })
})

describe('scheduling', () => {
    it('counts a play after half the track or four minutes', () => {
        expect(historyThreshold(60)).toBe(30)
        expect(historyThreshold(1200)).toBe(historyMaxThreshold)
        expect(historyThreshold(0)).toBe(historyMaxThreshold)
    })

    it('prepares the next track shortly before the end', () => {
        expect(shouldPreload(100, 200)).toBe(false)
        expect(shouldPreload(185, 200)).toBe(true)
        expect(shouldPreload(5, 0)).toBe(false)
    })

    it('restarts the track after some seconds instead of going back', () => {
        expect(shouldRestart(2)).toBe(false)
        expect(shouldRestart(10)).toBe(true)
    })

    it('counts small forward steps only', () => {
        expect(listenedBetween(10, 10.25)).toBe(0.25)
        expect(listenedBetween(10, 60)).toBe(0)
        expect(listenedBetween(60, 10)).toBe(0)
    })
})
