using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Resolutions;

/// <summary>
/// The metadata of a track, field by field from the tags, the folder and file name or the file name itself.
/// </summary>
public sealed record ResolvedTrackMetadata(
    string Title,
    TrackMetadataSource TitleSource,
    string? Artist,
    string? AlbumArtist,
    string? Album,
    int? TrackNumber,
    int? DiscNumber,
    int? Year,
    string? Genre);
