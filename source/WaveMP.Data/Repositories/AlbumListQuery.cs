using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filters of the album list.
/// </summary>
/// <param name="Offset">Number of albums to skip.</param>
/// <param name="Limit">Maximum number of albums of the page.</param>
/// <param name="Sort">Sort key; ties are sorted by title and id.</param>
/// <param name="Direction">Sort direction of the key.</param>
/// <param name="Search">Normalized search text: only albums whose title or album artist contains it or is similar
/// to it.</param>
/// <param name="ArtistId">Only albums with this album artist or with tracks of this artist.</param>
/// <param name="Genre">Only albums with tracks of this genre, without regard to case.</param>
/// <param name="Year">Only albums of this year.</param>
/// <param name="UserId">The user whose favorites the rows carry and to whom <paramref name="MyMusic"/> refers.</param>
/// <param name="MyMusic">Only albums with at least one track of the "my music" of the user. Needs
/// <paramref name="UserId"/>.</param>
public sealed record AlbumListQuery(
    int Offset,
    int Limit,
    AlbumSortField Sort,
    SortDirection Direction,
    string? Search = null,
    Guid? ArtistId = null,
    string? Genre = null,
    int? Year = null,
    Guid? UserId = null,
    bool MyMusic = false);
