namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// The kind of media a signed URL delivers. It is part of the signature, so a URL for the audio of a track is never
/// valid for its video.
/// </summary>
public enum MediaResource
{
    /// <summary>
    /// The audio file of a track (<c>GET /api/v1/tracks/{id}/stream</c>).
    /// </summary>
    TrackAudio,

    /// <summary>
    /// The video of a track (<c>GET /api/v1/tracks/{id}/video</c>).
    /// </summary>
    TrackVideo,

    /// <summary>
    /// The HLS stream of a track, for its sound and its video (<c>GET /api/v1/tracks/{id}/hls/...</c>): the master
    /// playlist, the media playlists and all segments share one signature, which the playlists append to each URI.
    /// </summary>
    TrackHls,
}
