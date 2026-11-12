using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filter of the genre list.
/// </summary>
/// <param name="Offset">Number of genres to skip.</param>
/// <param name="Limit">Maximum number of genres of the page.</param>
/// <param name="Sort">Sort key; ties are sorted by name.</param>
/// <param name="Direction">Sort direction of the key.</param>
/// <param name="Search">Normalized search text: only genres that contain it or are similar to it.</param>
/// <param name="UserId">The user to whom <paramref name="MyMusic"/> refers.</param>
/// <param name="MyMusic">Only the tracks of the "my music" of the user count. Needs <paramref name="UserId"/>.</param>
public sealed record GenreListQuery(
    int Offset,
    int Limit,
    GenreSortField Sort,
    SortDirection Direction,
    string? Search = null,
    Guid? UserId = null,
    bool MyMusic = false);
