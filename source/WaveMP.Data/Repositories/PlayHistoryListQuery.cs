namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging of the play history of a user; the newest play first, ties by id.
/// </summary>
/// <param name="UserId">Id of the user.</param>
/// <param name="Offset">Number of entries to skip.</param>
/// <param name="Limit">Maximum number of entries of the page.</param>
public sealed record PlayHistoryListQuery(Guid UserId, int Offset, int Limit);
