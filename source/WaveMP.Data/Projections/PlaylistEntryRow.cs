namespace WaveMP.Data.Projections;

/// <summary>
/// An entry of a playlist.
/// </summary>
/// <param name="Id">Id of the entry.</param>
/// <param name="TrackId">Id of the track.</param>
/// <param name="Position">Index of the entry in the current order of the playlist, starting at 0.</param>
/// <param name="AddedAt">When the entry was added.</param>
public sealed record PlaylistEntryRow(Guid Id, Guid TrackId, int Position, DateTimeOffset AddedAt);
