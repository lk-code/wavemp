namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// The kind of media a signed URL delivers. It is part of the signature, so a URL for the audio of a track is never
/// valid for its video. Step 12 adds the video and the HLS stream of a track.
/// </summary>
public enum MediaResource
{
    /// <summary>
    /// The audio file of a track (<c>GET /api/v1/tracks/{id}/stream</c>).
    /// </summary>
    TrackAudio,
}
