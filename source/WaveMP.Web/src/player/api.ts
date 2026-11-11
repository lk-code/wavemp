// Requests of the player. The only place of the web app that calls the playback and history
// endpoints, so the player store and its tests can replace it as a whole (vi.mock('@/player/api')).

import { apiClient } from '@/api/client'
import type {
    AudioFormat,
    PlayHistoryEntry,
    ReplayGainMode,
    TrackPlayback,
    UserSettings,
} from '@/api/generated/models'

/** How the client plays a track, with the signed stream URL for direct play. */
export function getPlayback(
    trackId: string,
    audioFormats: readonly AudioFormat[],
): Promise<TrackPlayback | undefined> {
    return apiClient.api.v1.tracks
        .byId(trackId)
        .playback.get({ queryParameters: { audioFormats: [...audioFormats] } })
}

/** Records a play in the history of the current profile. */
export function recordPlay(trackId: string, playedAt: Date): Promise<PlayHistoryEntry | undefined> {
    return apiClient.api.v1.me.history.post({ trackId, playedAt })
}

/** Saves the ReplayGain mode of the current profile. */
export function updateReplayGain(mode: ReplayGainMode): Promise<UserSettings | undefined> {
    return apiClient.api.v1.me.settings.patch({ replayGain: mode })
}
