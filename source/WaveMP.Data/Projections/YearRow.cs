namespace WaveMP.Data.Projections;

/// <summary>
/// A year of the tracks.
/// </summary>
/// <param name="Year">The year.</param>
/// <param name="TrackCount">Number of tracks of the year.</param>
/// <param name="AlbumCount">Number of albums with tracks of the year.</param>
public sealed record YearRow(int Year, int TrackCount, int AlbumCount);
