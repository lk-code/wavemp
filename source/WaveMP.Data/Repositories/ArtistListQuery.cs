using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filter of the artist list.
/// </summary>
/// <param name="Offset">Number of artists to skip.</param>
/// <param name="Limit">Maximum number of artists of the page.</param>
/// <param name="Sort">Sort key; ties are sorted by name and id.</param>
/// <param name="Direction">Sort direction of the key.</param>
/// <param name="Search">Normalized search text: only artists whose name contains it or is similar to it.</param>
/// <param name="UserId">The user whose favorites the rows carry and to whom <paramref name="MyMusic"/> refers.</param>
/// <param name="MyMusic">Only artists with at least one track of the "my music" of the user (as artist of the track or
/// of its album). Needs <paramref name="UserId"/>.</param>
public sealed record ArtistListQuery(
    int Offset,
    int Limit,
    ArtistSortField Sort,
    SortDirection Direction,
    string? Search = null,
    Guid? UserId = null,
    bool MyMusic = false);
