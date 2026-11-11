namespace WaveMP.Service.Dtos;

/// <summary>
/// A year of the tracks.
/// </summary>
public sealed class YearDto
{
    public required int Year { get; init; }

    public required int TrackCount { get; init; }

    /// <summary>
    /// Albums with tracks of the year.
    /// </summary>
    public required int AlbumCount { get; init; }
}
