namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging of the tracks a user played recently; each track once, the most recently played first.
/// </summary>
/// <param name="UserId">Id of the user.</param>
/// <param name="Offset">Number of tracks to skip.</param>
/// <param name="Limit">Maximum number of tracks of the page.</param>
/// <param name="MaxTracks">Number of tracks the whole list holds at most.</param>
public sealed record RecentTrackListQuery(Guid UserId, int Offset, int Limit, int MaxTracks);
