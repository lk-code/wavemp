namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// The raw values of the tag fields the metadata editor can change, as they are in the file (not normalized), e.g.
/// to keep them before the editor writes new tags. Missing numbers are <c>null</c>.
/// </summary>
public sealed record TagSnapshot(
    string? Title,
    IReadOnlyList<string> Performers,
    IReadOnlyList<string> AlbumArtists,
    string? Album,
    int? Year,
    int? TrackNumber,
    int? TrackCount,
    int? DiscNumber,
    int? DiscCount,
    IReadOnlyList<string> Genres);
