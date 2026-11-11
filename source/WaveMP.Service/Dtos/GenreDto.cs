namespace WaveMP.Service.Dtos;

/// <summary>
/// A genre of the tracks; genres that differ only in case are one genre.
/// </summary>
public sealed class GenreDto
{
    /// <summary>
    /// Name of the genre as found in the tracks; use it as filter genre of the lists.
    /// </summary>
    public required string Name { get; init; }

    public required int TrackCount { get; init; }

    /// <summary>
    /// Albums with tracks of the genre.
    /// </summary>
    public required int AlbumCount { get; init; }
}
