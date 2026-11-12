import { describe, expect, it } from 'vitest'
import {
    acceleratorKey,
    hardwareReasonKey,
    hardwareStatePresentation,
    segmentRatio,
} from '../transcoding'

describe('transcoding presentation', () => {
    it('labels the state of the hardware acceleration with a badge token', () => {
        expect(hardwareStatePresentation({ state: 'Active' })).toEqual({
            labelKey: 'adminTranscoding.hardware.states.active',
            token: 'active',
        })
        expect(hardwareStatePresentation(null)).toEqual({
            labelKey: 'adminTranscoding.hardware.states.detecting',
            token: 'detecting',
        })
    })

    it('translates the reasons and encoders of the API', () => {
        expect(hardwareReasonKey('no_device')).toBe('adminTranscoding.hardware.reasons.noDevice')
        expect(hardwareReasonKey(null)).toBeNull()
        expect(acceleratorKey('Vaapi')).toBe('adminTranscoding.accelerators.vaapi')
        expect(acceleratorKey(undefined)).toBe('adminTranscoding.accelerators.none')
    })

    it('computes the share of the parts that exist', () => {
        expect(segmentRatio({ completedSegments: 5, segmentCount: 20 })).toBe(0.25)
        expect(segmentRatio({ completedSegments: 30, segmentCount: 20 })).toBe(1)
        expect(segmentRatio({ completedSegments: 0, segmentCount: 0 })).toBeNull()
    })
})
