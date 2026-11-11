namespace WaveMP.Data.Projections;

/// <summary>
/// A track a user played recently.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="LastPlayedAt">When the user started the track for the last time.</param>
public sealed record RecentTrackRow(Guid TrackId, DateTimeOffset LastPlayedAt);
