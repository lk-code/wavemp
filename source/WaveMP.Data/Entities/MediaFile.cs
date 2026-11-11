namespace WaveMP.Data.Entities;

/// <summary>
/// A file of the media folder that the library uses: audio, video or picture. Every file is kept, also duplicates of
/// lower quality and pictures without a track, so a rescan can compare the folders with the database.
/// </summary>
public class MediaFile
{
    public Guid Id { get; set; }

    /// <summary>
    /// Path relative to the media folder with <c>/</c> as separator; unique.
    /// </summary>
    public required string RelativePath { get; set; }

    /// <summary>
    /// Folder relative to the media folder; empty for the media folder itself.
    /// </summary>
    public required string DirectoryPath { get; set; }

    public required string FileName { get; set; }

    /// <summary>
    /// File name without extension, as on disk.
    /// </summary>
    public required string BaseName { get; set; }

    /// <summary>
    /// Upper-case form of <see cref="BaseName"/>.
    /// </summary>
    public required string NormalizedBaseName { get; set; }

    /// <summary>
    /// Extension in lower case without the dot.
    /// </summary>
    public required string Extension { get; set; }

    public required MediaFileKind Kind { get; set; }

    /// <summary>
    /// Size in bytes; with <see cref="ModifiedAt"/> it decides whether the file changed.
    /// </summary>
    public required long Size { get; set; }

    /// <summary>
    /// Time of the last change, truncated to microseconds (the precision of PostgreSQL).
    /// </summary>
    public required DateTimeOffset ModifiedAt { get; set; }

    public Guid? TrackId { get; set; }

    public Track? Track { get; set; }

    /// <summary>
    /// The file of its category (audio or video) that the track plays: the one with the highest quality.
    /// </summary>
    public required bool IsPrimary { get; set; }

    /// <summary>
    /// Container format reported by ffprobe, e.g. <c>flac</c> or <c>mov,mp4,m4a,3gp,3g2,mj2</c>.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Codec of the main stream, e.g. <c>mp3</c>, <c>flac</c> or <c>h264</c>.
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// Bitrate in bit/s.
    /// </summary>
    public int? Bitrate { get; set; }

    public int? SampleRate { get; set; }

    public int? BitDepth { get; set; }

    public int? Channels { get; set; }

    public required bool IsLossless { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public TimeSpan? Duration { get; set; }

    public required bool HasEmbeddedPicture { get; set; }

    public int? EmbeddedPictureWidth { get; set; }

    public int? EmbeddedPictureHeight { get; set; }

    public string? EmbeddedPictureMimeType { get; set; }

    /// <summary>
    /// Whether the tags can be written back (audio files only; always <c>false</c> for videos and pictures). Set by
    /// the scan and also updated by the metadata editor whenever it checks the file.
    /// </summary>
    public required bool IsWritable { get; set; }

    /// <summary>
    /// The tags as read from the file.
    /// </summary>
    public MediaFileTags Tags { get; set; } = new();

    public MediaFileIssue? Issue { get; set; }

    /// <summary>
    /// Version of the metadata reader that read the file. Files read by an older version are read again.
    /// </summary>
    public required int MetadataVersion { get; set; }

    public required DateTimeOffset FirstSeenAt { get; set; }

    public required DateTimeOffset ScannedAt { get; set; }
}
