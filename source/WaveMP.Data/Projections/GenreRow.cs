namespace WaveMP.Data.Projections;

/// <summary>
/// A genre of the tracks; genres are compared without regard to case.
/// </summary>
/// <param name="Name">Name of the genre as found in the tracks.</param>
/// <param name="TrackCount">Number of tracks with the genre.</param>
/// <param name="AlbumCount">Number of albums with tracks of the genre.</param>
public sealed record GenreRow(string Name, int TrackCount, int AlbumCount);
