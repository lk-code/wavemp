using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Artists;

/// <summary>
/// A request for a page of artists as the client sends it.
/// </summary>
/// <param name="Offset">Number of artists to skip.</param>
/// <param name="Limit">Maximum number of artists of the page.</param>
/// <param name="Sort">Sort key; by default the name, or the relevance with a search text.</param>
/// <param name="Direction">Sort direction; by default descending for the relevance and the time found first.</param>
/// <param name="Search">Search text as typed; empty texts search nothing.</param>
public sealed record ArtistListRequest(
    int Offset,
    int Limit,
    ArtistSortField? Sort = null,
    SortDirection? Direction = null,
    string? Search = null);
