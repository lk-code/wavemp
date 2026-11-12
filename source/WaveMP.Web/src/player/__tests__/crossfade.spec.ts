import { describe, expect, it } from 'vitest'
import type { Track } from '@/api/generated/models'
import {
    crossfadeDuration,
    equalPowerCurve,
    toCrossfadeSettings,
    type CrossfadeSettings,
} from '../crossfade'

function track(album: string | null, seconds: number | null = 300): Track {
    return {
        album: album ? { id: album } : null,
        durationMilliseconds: seconds === null ? null : seconds * 1000,
    }
}

const on: CrossfadeSettings = { seconds: 6, albumsGapless: true }

describe('crossfade', () => {
    it('does not fade when it is off', () => {
        expect(
            crossfadeDuration({ seconds: 0, albumsGapless: false }, track('a'), track('b')),
        ).toBe(0)
    })

    it('fades between tracks of different albums', () => {
        expect(crossfadeDuration(on, track('a'), track('b'))).toBe(6)
    })

    it('keeps tracks of the same album gapless unless that is turned off', () => {
        expect(crossfadeDuration(on, track('a'), track('a'))).toBe(0)
        expect(crossfadeDuration({ ...on, albumsGapless: false }, track('a'), track('a'))).toBe(6)
    })

    it('fades tracks without album', () => {
        expect(crossfadeDuration(on, track(null), track(null))).toBe(6)
        expect(crossfadeDuration(on, track('a'), track(null))).toBe(6)
    })

    it('never takes more than half of either track', () => {
        expect(crossfadeDuration(on, track('a', 8), track('b'))).toBe(4)
        expect(crossfadeDuration(on, track('a'), track('b', 5))).toBe(2.5)
        expect(crossfadeDuration(on, track('a', 300), track('b'), 10)).toBe(5)
    })

    it('is not limited by unknown durations', () => {
        expect(crossfadeDuration(on, track('a', null), track('b', null))).toBe(6)
    })

    it('builds equal-power curves', () => {
        const fadeIn = equalPowerCurve('in', 5)
        const fadeOut = equalPowerCurve('out', 5)

        expect(fadeIn[0]).toBe(0)
        expect(fadeIn[4]).toBeCloseTo(1)
        expect(fadeOut[0]).toBe(1)
        expect(fadeOut[4]).toBeCloseTo(0)
        fadeIn.forEach((value, index) => {
            expect(value * value + fadeOut[index]! * fadeOut[index]!).toBeCloseTo(1, 5)
        })
    })

    it('reads complete settings only', () => {
        expect(toCrossfadeSettings({ seconds: 4, albumsGapless: false })).toEqual({
            seconds: 4,
            albumsGapless: false,
        })
        expect(toCrossfadeSettings({ seconds: 13, albumsGapless: true })).toBeNull()
        expect(toCrossfadeSettings({ seconds: 2.5, albumsGapless: true })).toBeNull()
        expect(toCrossfadeSettings(undefined)).toBeNull()
    })
})
