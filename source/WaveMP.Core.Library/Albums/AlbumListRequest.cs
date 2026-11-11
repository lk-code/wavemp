using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Albums;

/// <summary>
/// A request for a page of albums as the client sends it.
/// </summary>
/// <param name="Offset">Number of albums to skip.</param>
/// <param name="Limit">Maximum number of albums of the page.</param>
/// <param name="Sort">Sort key; by default the album artist, or the relevance with a search text.</param>
/// <param name="Direction">Sort direction; by default descending for the relevance and the time found first.</param>
/// <param name="Search">Search text as typed; empty texts search nothing.</param>
/// <param name="ArtistId">Only albums of this artist.</param>
/// <param name="Genre">Only albums with tracks of this genre; empty texts filter nothing.</param>
/// <param name="Year">Only albums of this year.</param>
public sealed record AlbumListRequest(
    int Offset,
    int Limit,
    AlbumSortField? Sort = null,
    SortDirection? Direction = null,
    string? Search = null,
    Guid? ArtistId = null,
    string? Genre = null,
    int? Year = null);
