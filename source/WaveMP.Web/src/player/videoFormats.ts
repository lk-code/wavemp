// The video formats the browser plays. The server decides by them between playing the video file
// as it is and an HLS stream it transcodes: the container, the codec of the picture and the codec
// of the sound each have to be supported. A browser tells by the MIME type with its codecs
// (canPlayType), so each value of the API has the type it is tested with.

import {
    AudioCodecObject,
    VideoCodecObject,
    VideoContainerObject,
    type AudioCodec,
    type VideoCodec,
    type VideoContainer,
} from '@/api/generated/models'
import type { CanPlayType } from './formats'

/** What the browser plays of videos, in the order of the API. */
export interface VideoFormats {
    readonly containers: VideoContainer[]
    readonly videoCodecs: VideoCodec[]
    readonly audioCodecs: AudioCodec[]
}

/** MIME type that canPlayType tests per container. */
export const videoContainerProbes: Readonly<Record<VideoContainer, string>> = {
    [VideoContainerObject.Mp4]: 'video/mp4',
    [VideoContainerObject.Mov]: 'video/quicktime',
    [VideoContainerObject.Webm]: 'video/webm',
    [VideoContainerObject.Matroska]: 'video/x-matroska',
}

/** MIME type with codecs that canPlayType tests per codec of the picture (8 bit, common levels). */
export const videoCodecProbes: Readonly<Record<VideoCodec, string>> = {
    [VideoCodecObject.H264]: 'video/mp4; codecs="avc1.640028"',
    [VideoCodecObject.Hevc]: 'video/mp4; codecs="hvc1.1.6.L120.90"',
    [VideoCodecObject.Vp8]: 'video/webm; codecs="vp8"',
    [VideoCodecObject.Vp9]: 'video/webm; codecs="vp09.00.40.08"',
    [VideoCodecObject.Av1]: 'video/mp4; codecs="av01.0.08M.08"',
}

/** MIME type with codecs that canPlayType tests per codec of the sound of a video. */
export const videoAudioCodecProbes: Readonly<Record<AudioCodec, string>> = {
    [AudioCodecObject.Aac]: 'video/mp4; codecs="mp4a.40.2"',
    [AudioCodecObject.Mp3]: 'video/mp4; codecs="mp4a.6B"',
    [AudioCodecObject.Opus]: 'video/webm; codecs="opus"',
    [AudioCodecObject.Vorbis]: 'video/webm; codecs="vorbis"',
    [AudioCodecObject.Flac]: 'video/mp4; codecs="flac"',
    [AudioCodecObject.Ac3]: 'video/mp4; codecs="ac-3"',
    [AudioCodecObject.Eac3]: 'video/mp4; codecs="ec-3"',
}

function supported<T extends string>(
    probes: Readonly<Record<T, string>>,
    canPlayType: CanPlayType,
) {
    return (Object.keys(probes) as T[]).filter((value) => canPlayType(probes[value]) !== '')
}

/**
 * The video formats the probe can play. "maybe" counts, as for audio: a video that fails to play
 * anyway shows an error.
 */
export function supportedVideoFormats(canPlayType: CanPlayType): VideoFormats {
    return {
        containers: supported(videoContainerProbes, canPlayType),
        videoCodecs: supported(videoCodecProbes, canPlayType),
        audioCodecs: supported(videoAudioCodecProbes, canPlayType),
    }
}

let detected: VideoFormats | null = null

/** The video formats of this browser, tested once with a video element. */
export function browserVideoFormats(): VideoFormats {
    if (detected === null) {
        const video = typeof document === 'undefined' ? null : document.createElement('video')
        detected = video
            ? supportedVideoFormats((type) => video.canPlayType(type))
            : { containers: [], videoCodecs: [], audioCodecs: [] }
    }

    return detected
}
