namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging of the entries of a playlist, in the order of the playlist.
/// </summary>
/// <param name="UserId">Id of the user the playlist must belong to.</param>
/// <param name="PlaylistId">Id of the playlist.</param>
/// <param name="Offset">Number of entries to skip.</param>
/// <param name="Limit">Maximum number of entries of the page.</param>
public sealed record PlaylistEntryListQuery(Guid UserId, Guid PlaylistId, int Offset, int Limit);
