namespace WaveMP.Service.Dtos;

/// <summary>
/// The metadata of a track as the metadata editor shows it: the values of the tags of its audio file; values the
/// tags do not contain come from the library (e.g. derived from the folder and file name).
/// </summary>
public sealed class TrackMetadataDto
{
    /// <summary>
    /// Id of the track.
    /// </summary>
    public required Guid TrackId { get; init; }

    /// <summary>
    /// Whether the editor can write the tags.
    /// </summary>
    public required TrackMetadataStatusDto Status { get; init; }

    /// <summary>
    /// Path of the audio file the editor writes, relative to the media folder; null without audio file.
    /// </summary>
    public string? AudioFilePath { get; init; }

    public required string Title { get; init; }

    public string? Artist { get; init; }

    public string? Album { get; init; }

    public string? AlbumArtist { get; init; }

    public int? Year { get; init; }

    /// <summary>
    /// Number of the track on its disc.
    /// </summary>
    public int? TrackNumber { get; init; }

    public int? DiscNumber { get; init; }

    public string? Genre { get; init; }
}
