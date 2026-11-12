namespace WaveMP.Data.Entities;

/// <summary>
/// A track: all media files with the same base name in the same folder (e.g. <c>Song.flac</c>, <c>Song.mkv</c> and
/// <c>Song.jpg</c>), with its metadata from tags, folder and file names.
/// </summary>
public class Track
{
    public Guid Id { get; set; }

    /// <summary>
    /// Folder relative to the media folder with <c>/</c> as separator; empty for the media folder itself.
    /// </summary>
    public required string DirectoryPath { get; set; }

    /// <summary>
    /// File name without extension, as on disk.
    /// </summary>
    public required string BaseName { get; set; }

    /// <summary>
    /// Upper-case form of <see cref="BaseName"/>; files are grouped without regard to case.
    /// </summary>
    public required string NormalizedBaseName { get; set; }

    public required string Title { get; set; }

    public Guid? ArtistId { get; set; }

    public Artist? Artist { get; set; }

    public Guid? AlbumId { get; set; }

    public Album? Album { get; set; }

    public int? TrackNumber { get; set; }

    public int? DiscNumber { get; set; }

    public int? Year { get; set; }

    public string? Genre { get; set; }

    /// <summary>
    /// Duration of the primary audio file, otherwise of the primary video file.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    public required bool HasAudio { get; set; }

    public required bool HasVideo { get; set; }

    /// <summary>
    /// Where the title comes from.
    /// </summary>
    public required TrackMetadataSource MetadataSource { get; set; }

    /// <summary>
    /// When the track was found for the first time; never changed (basis of "recently added").
    /// </summary>
    public required DateTimeOffset FirstSeenAt { get; set; }

    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Offset of the video against the audio in milliseconds: position of the video = position of the audio + offset.
    /// Set by the admin for videos whose music starts earlier or later than in the audio file; a scan never changes it.
    /// </summary>
    public int VideoOffsetMilliseconds { get; set; }

    public ICollection<MediaFile> Files { get; set; } = [];

    public ICollection<Cover> Covers { get; set; } = [];
}
