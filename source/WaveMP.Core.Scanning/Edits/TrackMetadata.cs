namespace WaveMP.Core.Scanning.Edits;

/// <summary>
/// The metadata of a track as the metadata editor shows it: the values of the tags of its audio file; values the tags
/// do not contain come from the library (e.g. derived from the folder and file name).
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="Status">Whether the editor can write the tags.</param>
/// <param name="AudioFilePath">The audio file the editor writes, relative to the media folder; <c>null</c> without
/// audio file.</param>
/// <param name="Title">Title.</param>
/// <param name="Artist">Artist.</param>
/// <param name="Album">Album.</param>
/// <param name="AlbumArtist">Album artist.</param>
/// <param name="Year">Year.</param>
/// <param name="TrackNumber">Number of the track on its disc.</param>
/// <param name="DiscNumber">Number of the disc.</param>
/// <param name="Genre">Genre.</param>
public sealed record TrackMetadata(
    Guid TrackId,
    TrackMetadataStatus Status,
    string? AudioFilePath,
    string Title,
    string? Artist,
    string? Album,
    string? AlbumArtist,
    int? Year,
    int? TrackNumber,
    int? DiscNumber,
    string? Genre);
