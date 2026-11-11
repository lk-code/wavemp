namespace WaveMP.Data.Entities;

/// <summary>
/// The tags of an audio file right before the metadata editor wrote new tags into it: the raw values of the fields
/// the editor can change. One backup per write; the oldest backup of a file holds its original tags. A backup stays
/// when its file leaves the library (<see cref="MediaFileId"/> becomes <c>null</c>), so the original values are never
/// lost.
/// </summary>
public class TagBackup
{
    public Guid Id { get; set; }

    public Guid? MediaFileId { get; set; }

    public MediaFile? MediaFile { get; set; }

    /// <summary>
    /// Path of the file relative to the media folder at the time of the backup.
    /// </summary>
    public required string RelativePath { get; set; }

    public string? Title { get; set; }

    /// <summary>
    /// All artists of the file.
    /// </summary>
    public string[] Performers { get; set; } = [];

    /// <summary>
    /// All album artists of the file.
    /// </summary>
    public string[] AlbumArtists { get; set; } = [];

    public string? Album { get; set; }

    public int? Year { get; set; }

    public int? TrackNumber { get; set; }

    public int? TrackCount { get; set; }

    public int? DiscNumber { get; set; }

    public int? DiscCount { get; set; }

    /// <summary>
    /// All genres of the file.
    /// </summary>
    public string[] Genres { get; set; } = [];

    public required DateTimeOffset CreatedAt { get; set; }
}
