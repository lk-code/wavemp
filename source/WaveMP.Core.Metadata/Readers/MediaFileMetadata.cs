using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Readers;

/// <summary>
/// Everything WaveMP reads from a media file: tags, technical information and quality, the embedded picture and
/// whether the tags can be written back.
/// </summary>
public sealed record MediaFileMetadata
{
    public MediaTags Tags { get; init; } = MediaTags.Empty;

    public string? Format { get; init; }

    public string? Codec { get; init; }

    /// <summary>
    /// Codec of the first audio stream of a video; <c>null</c> for videos without sound and for other files.
    /// </summary>
    public string? AudioCodec { get; init; }

    /// <summary>
    /// Pixel format of the video stream of a video, e.g. <c>yuv420p</c>; <c>null</c> for other files.
    /// </summary>
    public string? PixelFormat { get; init; }

    /// <summary>
    /// Bitrate in bit/s: of the audio stream for audio files, of the video stream (or the file) for videos.
    /// </summary>
    public int? Bitrate { get; init; }

    public int? SampleRate { get; init; }

    public int? BitDepth { get; init; }

    public int? Channels { get; init; }

    public bool IsLossless { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public TimeSpan? Duration { get; init; }

    public bool HasEmbeddedPicture { get; init; }

    public int? EmbeddedPictureWidth { get; init; }

    public int? EmbeddedPictureHeight { get; init; }

    public string? EmbeddedPictureMimeType { get; init; }

    /// <summary>
    /// Whether the tags can be written back; only checked for audio files.
    /// </summary>
    public bool IsWritable { get; init; }

    public MediaFileIssue? Issue { get; init; }
}
