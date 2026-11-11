namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// Technical information about a media file as reported by ffprobe.
/// </summary>
/// <param name="FormatName">Container format, e.g. <c>flac</c> or <c>matroska,webm</c>.</param>
/// <param name="Duration">Duration of the file.</param>
/// <param name="Bitrate">Overall bitrate in bit/s.</param>
/// <param name="Tags">Tags of the container and of the first audio stream, keys in lower case.</param>
/// <param name="Audio">The first audio stream.</param>
/// <param name="Video">The first video stream that is not an attached picture; for pictures the picture itself.</param>
/// <param name="AttachedPicture">A picture embedded as stream (cover of audio files).</param>
public sealed record MediaProbeResult(
    string? FormatName,
    TimeSpan? Duration,
    long? Bitrate,
    IReadOnlyDictionary<string, string> Tags,
    AudioStreamInfo? Audio,
    VideoStreamInfo? Video,
    PictureStreamInfo? AttachedPicture);
