using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filter of the playlists of a user.
/// </summary>
/// <param name="UserId">Id of the user.</param>
/// <param name="Offset">Number of playlists to skip.</param>
/// <param name="Limit">Maximum number of playlists of the page.</param>
/// <param name="Sort">Sort key; ties are sorted by id.</param>
/// <param name="Direction">Sort direction of the key.</param>
/// <param name="Search">Normalized search text: only playlists whose name contains it or is similar to it.</param>
public sealed record PlaylistListQuery(
    Guid UserId,
    int Offset,
    int Limit,
    PlaylistSortField Sort = PlaylistSortField.Name,
    SortDirection Direction = SortDirection.Asc,
    string? Search = null);
