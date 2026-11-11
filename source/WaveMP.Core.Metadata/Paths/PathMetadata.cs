namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// Metadata derived from the folder and file name of a media file.
/// </summary>
/// <param name="Title">Title from the file name; the cleaned file name if no pattern applies.</param>
/// <param name="IsTitleFromPattern">Whether the title is a part of the file name rather than all of it.</param>
/// <param name="Artist">Artist from the file name, otherwise from the folders.</param>
/// <param name="AlbumArtist">The artist of the folder if the file name names another artist (e.g. samplers).</param>
/// <param name="Album">Album from the file name, otherwise from the folders.</param>
/// <param name="TrackNumber">Track number from the file name.</param>
/// <param name="DiscNumber">Disc number from the file name or a disc folder such as <c>CD2</c>.</param>
/// <param name="Year">Year from the album folder, e.g. <c>2001 - Album</c>.</param>
public sealed record PathMetadata(
    string Title,
    bool IsTitleFromPattern,
    string? Artist = null,
    string? AlbumArtist = null,
    string? Album = null,
    int? TrackNumber = null,
    int? DiscNumber = null,
    int? Year = null);
