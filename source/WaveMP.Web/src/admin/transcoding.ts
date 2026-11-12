// Presentation of the transcoding for the admin page: message keys of the codes of the API and the
// values the settings offer. Pure functions.

import {
    HardwareAccelerationStateObject,
    HardwareAcceleratorObject,
    HardwareUnavailableReasonObject,
    type HardwareAcceleration,
    type HardwareAccelerationState,
    type HardwareAccelerator,
    type HardwareUnavailableReason,
    type TranscodeJob,
} from '@/api/generated/models'

/** Values of the settings, as the API accepts them. */
export const maxParallelJobOptions: readonly number[] = [1, 2, 3, 4, 5, 6, 7, 8]
export const maxVideoHeightOptions: readonly number[] = [480, 720, 1080, 1440, 2160]
export const audioBitrateOptions: readonly number[] = [128, 160, 192, 256, 320]

const stateKeys: Readonly<Record<HardwareAccelerationState, string>> = {
    [HardwareAccelerationStateObject.Detecting]: 'adminTranscoding.hardware.states.detecting',
    [HardwareAccelerationStateObject.Active]: 'adminTranscoding.hardware.states.active',
    [HardwareAccelerationStateObject.Unavailable]: 'adminTranscoding.hardware.states.unavailable',
    [HardwareAccelerationStateObject.Disabled]: 'adminTranscoding.hardware.states.disabled',
    [HardwareAccelerationStateObject.Failed]: 'adminTranscoding.hardware.states.failed',
}

const reasonKeys: Readonly<Record<HardwareUnavailableReason, string>> = {
    [HardwareUnavailableReasonObject.Ffmpeg_missing]:
        'adminTranscoding.hardware.reasons.ffmpegMissing',
    [HardwareUnavailableReasonObject.No_device]: 'adminTranscoding.hardware.reasons.noDevice',
    [HardwareUnavailableReasonObject.Test_failed]: 'adminTranscoding.hardware.reasons.testFailed',
    [HardwareUnavailableReasonObject.Encode_failed]:
        'adminTranscoding.hardware.reasons.encodeFailed',
}

const acceleratorKeys: Readonly<Record<HardwareAccelerator, string>> = {
    [HardwareAcceleratorObject.None]: 'adminTranscoding.accelerators.none',
    [HardwareAcceleratorObject.Vaapi]: 'adminTranscoding.accelerators.vaapi',
    [HardwareAcceleratorObject.Qsv]: 'adminTranscoding.accelerators.qsv',
}

/**
 * Label and badge token of the state of the hardware acceleration; the token is the state in lower
 * case (detecting, active, unavailable, disabled, failed).
 */
export function hardwareStatePresentation(hardware: HardwareAcceleration | null | undefined): {
    labelKey: string
    token: string
} {
    const state = hardware?.state ?? HardwareAccelerationStateObject.Detecting
    return { labelKey: stateKeys[state], token: state.toLowerCase() }
}

/** Message key of why the hardware is not used; null without a reason. */
export function hardwareReasonKey(
    reason: HardwareUnavailableReason | null | undefined,
): string | null {
    return reason ? reasonKeys[reason] : null
}

/** Message key of the encoder of a transcode or of the hardware. */
export function acceleratorKey(accelerator: HardwareAccelerator | null | undefined): string {
    return acceleratorKeys[accelerator ?? HardwareAcceleratorObject.None]
}

/** Share of the segments of a transcode that exist, from its start segment on; null if unknown. */
export function segmentRatio(job: TranscodeJob): number | null {
    const total = job.segmentCount ?? 0
    if (total <= 0) {
        return null
    }

    return Math.min(1, Math.max(0, (job.completedSegments ?? 0) / total))
}
