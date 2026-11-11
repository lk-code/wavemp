// The audio formats the browser plays. The server decides by them between direct play and
// transcoding; a browser tells by the MIME type with its codecs (canPlayType), so each format of
// the API has the type it is tested with. Pure functions besides the default probe.

import { AudioFormatObject, type AudioFormat } from '@/api/generated/models'

/** MIME type with codecs that canPlayType tests per format. */
export const audioFormatProbes: Readonly<Record<AudioFormat, string>> = {
    [AudioFormatObject.Mp3]: 'audio/mpeg',
    [AudioFormatObject.Flac]: 'audio/flac',
    [AudioFormatObject.OggFlac]: 'audio/ogg; codecs="flac"',
    [AudioFormatObject.AdtsAac]: 'audio/aac',
    [AudioFormatObject.Mp4Aac]: 'audio/mp4; codecs="mp4a.40.2"',
    [AudioFormatObject.Mp4Alac]: 'audio/mp4; codecs="alac"',
    [AudioFormatObject.OggOpus]: 'audio/ogg; codecs="opus"',
    [AudioFormatObject.OggVorbis]: 'audio/ogg; codecs="vorbis"',
    [AudioFormatObject.WebmOpus]: 'audio/webm; codecs="opus"',
    [AudioFormatObject.WebmVorbis]: 'audio/webm; codecs="vorbis"',
    [AudioFormatObject.WavPcm]: 'audio/wav; codecs="1"',
    [AudioFormatObject.AiffPcm]: 'audio/aiff',
}

/** Answer of canPlayType: "", "maybe" or "probably". */
export type CanPlayType = (type: string) => string

/**
 * The formats the probe can play, in the order of the API. "maybe" counts: browsers answer it for
 * most types without codecs, e.g. audio/mpeg. A format that fails to play anyway shows an error.
 */
export function supportedAudioFormats(canPlayType: CanPlayType): AudioFormat[] {
    return (Object.keys(audioFormatProbes) as AudioFormat[]).filter(
        (format) => canPlayType(audioFormatProbes[format]) !== '',
    )
}

let detected: AudioFormat[] | null = null

/** The formats of this browser, tested once with an audio element. */
export function browserAudioFormats(): AudioFormat[] {
    if (detected === null) {
        const audio = typeof document === 'undefined' ? null : document.createElement('audio')
        detected = audio ? supportedAudioFormats((type) => audio.canPlayType(type)) : []
    }

    return detected
}
