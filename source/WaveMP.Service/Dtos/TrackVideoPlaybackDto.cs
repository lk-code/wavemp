namespace WaveMP.Service.Dtos;

/// <summary>
/// How the client plays the video of a track, and the signed URL of its stream.
/// </summary>
public sealed class TrackVideoPlaybackDto
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
    /// The file as it is (direct play) or an HLS stream (transcoding).
    /// </summary>
    public required StreamProtocolDto Protocol { get; init; }

    /// <summary>
    /// Why the video has to be transcoded; null for direct play.
    /// </summary>
    public TranscodeReasonDto? TranscodeReason { get; init; }

    /// <summary>
    /// Container of the file; null if it is none of the containers clients play directly.
    /// </summary>
    public VideoContainerDto? Container { get; init; }

    /// <summary>
    /// Codec of the picture; null if it is none of the codecs clients play directly.
    /// </summary>
    public VideoCodecDto? VideoCodec { get; init; }

    /// <summary>
    /// Codec of the sound; null without sound or if it is none of the codecs clients play directly.
    /// </summary>
    public AudioCodecDto? AudioCodec { get; init; }

    /// <summary>
    /// Whether the video has sound.
    /// </summary>
    public required bool HasAudio { get; init; }

    /// <summary>
    /// Width of the picture of the file in pixels, if known.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height of the picture of the file in pixels, if known.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// MIME type the stream is delivered with; null without stream.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Signed, short-lived URL of the stream (path with query, relative to the server), for players that cannot send
    /// the session token: the file for direct play, the HLS master playlist for transcoding. Null if the video has to be
    /// transcoded and transcoding is unavailable. It stays valid until expiresAt and while the session is active.
    /// </summary>
    public string? StreamUrl { get; init; }

    /// <summary>
    /// When the stream URL expires; null without stream.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Duration of the video in milliseconds, if known.
    /// </summary>
    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// Offset of the video against the audio of the track in milliseconds: position of the video = position of the
    /// audio + offset; 0 without offset. Clients that play the audio and show the video muted synchronize the video
    /// with it.
    /// </summary>
    public required int OffsetMilliseconds { get; init; }
}
