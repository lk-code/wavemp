using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tracks;

/// <summary>
/// A request for a page of the genres of the tracks.
/// </summary>
/// <param name="Offset">Number of genres to skip.</param>
/// <param name="Limit">Maximum number of genres of the page.</param>
/// <param name="Sort">Sort key; by default the name.</param>
/// <param name="Direction">Sort direction; by default descending for the number of tracks.</param>
/// <param name="Search">Search text as typed; empty texts search nothing.</param>
/// <param name="UserId">The profile to whom <paramref name="MyMusic"/> refers.</param>
/// <param name="MyMusic">Only the tracks of the "my music" of the profile count; ignored without a profile.</param>
public sealed record GenreListRequest(
    int Offset,
    int Limit,
    GenreSortField? Sort = null,
    SortDirection? Direction = null,
    string? Search = null,
    Guid? UserId = null,
    bool MyMusic = false);
