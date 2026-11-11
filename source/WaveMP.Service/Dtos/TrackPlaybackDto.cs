namespace WaveMP.Service.Dtos;

/// <summary>
/// How the client plays the audio of a track, and the signed URL of its stream.
/// </summary>
public sealed class TrackPlaybackDto
{
    /// <summary>
    /// Id of the track.
    /// </summary>
    public required Guid TrackId { get; init; }

    /// <summary>
    /// Direct play or transcoding.
    /// </summary>
    public required PlaybackMethodDto Method { get; init; }

    /// <summary>
    /// Format of the audio file; null if it is none of the formats clients play directly.
    /// </summary>
    public AudioFormatDto? Format { get; init; }

    /// <summary>
    /// Why the track has to be transcoded; null for direct play.
    /// </summary>
    public TranscodeReasonDto? TranscodeReason { get; init; }

    /// <summary>
    /// MIME type the stream is delivered with; null without stream.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Signed, short-lived URL of the stream (path with query, relative to the server), for players that cannot send
    /// the session token; null if the track has to be transcoded. It stays valid until expiresAt and while the
    /// session is active.
    /// </summary>
    public string? StreamUrl { get; init; }

    /// <summary>
    /// When the stream URL expires; null without stream.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Duration of the audio file in milliseconds, if known.
    /// </summary>
    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// The ReplayGain tags of the audio file.
    /// </summary>
    public required ReplayGainDto ReplayGain { get; init; }
}
