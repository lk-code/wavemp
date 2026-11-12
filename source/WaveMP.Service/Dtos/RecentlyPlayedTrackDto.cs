namespace WaveMP.Service.Dtos;

/// <summary>
/// A track of the automatic list "Recently played": every track once, at its last play.
/// </summary>
public sealed class RecentlyPlayedTrackDto
{
    /// <summary>
    /// When the last play of the track started.
    /// </summary>
    public required DateTimeOffset LastPlayedAt { get; init; }

    public required TrackDto Track { get; init; }
}
