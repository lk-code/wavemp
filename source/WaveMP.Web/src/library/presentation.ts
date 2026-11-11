// Presentation of the library: message keys and data-state tokens for tracks, artists and files.
// Pure functions only.

import type { ArtistReference, Track } from '@/api/generated/models'

/**
 * Message key for an artist without name: "Various artists" for compilations, otherwise "Unknown
 * artist". Null if the artist has a name to show.
 */
export function artistLabelKey(
    artist: ArtistReference | null | undefined,
    isCompilation = false,
): string | null {
    if (artist?.name) {
        return null
    }

    return isCompilation ? 'library.variousArtists' : 'library.unknownArtist'
}

/** Duration in seconds from milliseconds; null if unknown. */
export function durationSeconds(milliseconds: number | null | undefined): number | null {
    return typeof milliseconds === 'number' && Number.isFinite(milliseconds)
        ? Math.max(0, milliseconds / 1000)
        : null
}

/** data-state tokens of the row of a track: a video next to the audio, or only a video. */
export function trackRowState(track: Pick<Track, 'hasAudio' | 'hasVideo'>): string[] {
    if (!track.hasVideo) {
        return []
    }

    return track.hasAudio ? ['has-video'] : ['video-only']
}

/** Message key of how a track can be played: audio and video, audio only or video only. */
export function mediaKey(track: Pick<Track, 'hasAudio' | 'hasVideo'>): string {
    if (track.hasAudio && track.hasVideo) {
        return 'track.media.audioAndVideo'
    }

    return track.hasVideo ? 'track.media.video' : 'track.media.audio'
}

const fileKinds: Record<string, string> = {
    Audio: 'audio',
    Video: 'video',
    Image: 'image',
}

/** data-state token of the kind of a file: audio, video or image. */
export function fileKindToken(kind: string | null | undefined): string {
    return fileKinds[kind ?? ''] ?? 'audio'
}

/** Message key of the kind of a file. */
export function fileKindKey(kind: string | null | undefined): string {
    return `files.kinds.${fileKindToken(kind)}`
}
