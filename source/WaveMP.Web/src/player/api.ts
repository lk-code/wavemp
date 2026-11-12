// Requests of the player. The only place of the web app that calls the playback and history
// endpoints, so the player store and its tests can replace it as a whole (vi.mock('@/player/api')).
// The settings of the profile (ReplayGain, equalizer, crossfade) belong to the session store.

import { apiClient } from '@/api/client'
import type {
    AudioFormat,
    Equalizer,
    PlayHistoryEntry,
    TrackPlayback,
    TrackVideoPlayback,
} from '@/api/generated/models'
import type { VideoFormats } from './videoFormats'

/** How the client plays a track, with the signed stream URL for direct play. */
export function getPlayback(
    trackId: string,
    audioFormats: readonly AudioFormat[],
): Promise<TrackPlayback | undefined> {
    return apiClient.api.v1.tracks
        .byId(trackId)
        .playback.get({ queryParameters: { audioFormats: [...audioFormats] } })
}

/** How the client plays the video of a track: the file as it is or an HLS stream. */
export function getVideoPlayback(
    trackId: string,
    formats: VideoFormats,
): Promise<TrackVideoPlayback | undefined> {
    return apiClient.api.v1.tracks.byId(trackId).video.playback.get({
        queryParameters: {
            videoContainers: [...formats.containers],
            videoCodecs: [...formats.videoCodecs],
            audioCodecs: [...formats.audioCodecs],
        },
    })
}

/** Records a play in the history of the current profile. */
export function recordPlay(trackId: string, playedAt: Date): Promise<PlayHistoryEntry | undefined> {
    return apiClient.api.v1.me.history.post({ trackId, playedAt })
}

/** The equalizer of the server: bands, limits and the curves of the presets. */
export function getEqualizer(): Promise<Equalizer | undefined> {
    return apiClient.api.v1.equalizer.get()
}
