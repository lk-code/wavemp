namespace WaveMP.Service.Dtos;

/// <summary>
/// A file of a track as found by the scan, with its technical details.
/// </summary>
public sealed class MediaFileDto
{
    public required Guid Id { get; init; }

    public required MediaFileKindDto Kind { get; init; }

    /// <summary>
    /// Path relative to the media folder with / as separator.
    /// </summary>
    public required string RelativePath { get; init; }

    public required string FileName { get; init; }

    /// <summary>
    /// Extension in lower case without the dot.
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// Size in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Time of the last change of the file.
    /// </summary>
    public required DateTimeOffset ModifiedAt { get; init; }

    /// <summary>
    /// The file of its kind (audio or video) that the track plays: the one with the highest quality.
    /// </summary>
    public required bool IsPrimary { get; init; }

    /// <summary>
    /// Container format reported by ffprobe, e.g. flac.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Codec of the main stream, e.g. mp3, flac or h264.
    /// </summary>
    public string? Codec { get; init; }

    /// <summary>
    /// Bitrate in bit/s.
    /// </summary>
    public int? Bitrate { get; init; }

    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int? SampleRate { get; init; }

    public int? BitDepth { get; init; }

    public int? Channels { get; init; }

    public required bool IsLossless { get; init; }

    /// <summary>
    /// Width in pixels of videos and pictures.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height in pixels of videos and pictures.
    /// </summary>
    public int? Height { get; init; }

    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// Whether the tags of the file contain a picture.
    /// </summary>
    public required bool HasEmbeddedPicture { get; init; }

    /// <summary>
    /// Whether the tags can be written back (audio files only).
    /// </summary>
    public required bool IsWritable { get; init; }

    /// <summary>
    /// A problem found while reading the file; null if there was none.
    /// </summary>
    public ScanIssueCodeDto? Issue { get; init; }

    /// <summary>
    /// When the file was found for the first time.
    /// </summary>
    public required DateTimeOffset FirstSeenAt { get; init; }

    /// <summary>
    /// When the file was read the last time.
    /// </summary>
    public required DateTimeOffset ScannedAt { get; init; }
}
