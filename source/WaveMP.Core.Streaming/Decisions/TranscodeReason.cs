namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Why a track has to be transcoded.
/// </summary>
public enum TranscodeReason
{
    /// <summary>
    /// The client does not support the format of the file.
    /// </summary>
    FormatUnsupported,

    /// <summary>
    /// The format of the file is not one clients can play directly (see <see cref="Formats.AudioFormats"/> and
    /// <see cref="Formats.VideoFormats"/>), or it is not known yet.
    /// </summary>
    FormatUnknown,

    /// <summary>
    /// The client does not support the container of the video.
    /// </summary>
    ContainerUnsupported,

    /// <summary>
    /// The client does not support the codec of the picture of the video (or its variant, e.g. H.264 with 10 bit).
    /// </summary>
    VideoCodecUnsupported,

    /// <summary>
    /// The client does not support the codec of the sound of the video.
    /// </summary>
    AudioCodecUnsupported,

    /// <summary>
    /// The track has no audio file; its sound comes from the video.
    /// </summary>
    AudioFromVideo,
}
