import { describe, expect, it } from 'vitest'
import {
    endMargin,
    hardSyncThreshold,
    maxRateCorrection,
    minSeekInterval,
    softSyncThreshold,
    stageViewOf,
    syncAction,
    videoTarget,
    type SyncInput,
} from '../videoSync'

function input(extra: Partial<SyncInput> = {}): SyncInput {
    return {
        audioPosition: 30,
        playing: true,
        videoPosition: 30,
        offset: 0,
        videoDuration: 200,
        sinceLastSeek: 10,
        ...extra,
    }
}

describe('video sync', () => {
    it('adds the offset to the position of the audio', () => {
        expect(videoTarget(30, 1.5)).toBe(31.5)
        expect(videoTarget(30, -2)).toBe(28)
    })

    it('leaves a video in sync at its normal speed', () => {
        expect(syncAction(input({ videoPosition: 30 + softSyncThreshold / 2 }))).toEqual({
            kind: 'rate',
            rate: 1,
        })
    })

    it('holds the first picture until the music of the video starts', () => {
        expect(syncAction(input({ audioPosition: 1, offset: -3, videoPosition: 0 }))).toEqual({
            kind: 'hold',
            at: 0,
        })
    })

    it('holds the last picture once the video ended before the audio', () => {
        expect(syncAction(input({ audioPosition: 199, offset: 2 }))).toEqual({
            kind: 'hold',
            at: 200 - endMargin,
        })
    })

    it('seeks from the hard threshold on, with the offset', () => {
        expect(syncAction(input({ videoPosition: 30, offset: hardSyncThreshold + 0.1 }))).toEqual({
            kind: 'seek',
            to: 30 + (hardSyncThreshold + 0.1),
        })
        expect(syncAction(input({ audioPosition: 40, videoPosition: 30, offset: 1.5 }))).toEqual({
            kind: 'seek',
            to: 41.5,
        })
    })

    it('does not seek again within the minimum interval but changes the speed', () => {
        const action = syncAction(input({ videoPosition: 32, sinceLastSeek: minSeekInterval / 2 }))

        expect(action).toEqual({ kind: 'rate', rate: 1 - maxRateCorrection })
    })

    it('slows down a video that is a little ahead and speeds up one that is behind', () => {
        const ahead = syncAction(input({ videoPosition: 30.1 }))
        const behind = syncAction(input({ videoPosition: 29.9 }))

        expect(ahead.kind).toBe('rate')
        expect(ahead.kind === 'rate' && ahead.rate).toBeCloseTo(0.95)
        expect(behind.kind === 'rate' && behind.rate).toBeCloseTo(1.05)
    })

    it('limits the change of the speed', () => {
        const action = syncAction(input({ videoPosition: 29.75 }))

        expect(action).toEqual({ kind: 'rate', rate: 1 + maxRateCorrection })
    })

    it('seeks a paused video to the exact position', () => {
        expect(syncAction(input({ playing: false, videoPosition: 30.1 }))).toEqual({
            kind: 'seek',
            to: 30,
        })
    })

    it('ignores the end while the duration of the video is unknown', () => {
        expect(
            syncAction(input({ videoDuration: 0, audioPosition: 500, videoPosition: 500 })),
        ).toEqual({ kind: 'rate', rate: 1 })
    })
})

describe('stage view', () => {
    it('shows the video only when the track has one and it is ready', () => {
        expect(stageViewOf('Video', true, 'ready')).toBe('video')
        expect(stageViewOf('Video', true, 'loading')).toBe('cover')
        expect(stageViewOf('Video', true, 'failed')).toBe('cover')
        expect(stageViewOf('Video', true, 'idle')).toBe('cover')
        expect(stageViewOf('Video', false, 'ready')).toBe('cover')
    })

    it('shows the cover and the visualization as chosen', () => {
        expect(stageViewOf('Cover', true, 'ready')).toBe('cover')
        expect(stageViewOf('Visualization', false, 'idle')).toBe('visualization')
        expect(stageViewOf('Visualization', true, 'ready')).toBe('visualization')
    })
})
