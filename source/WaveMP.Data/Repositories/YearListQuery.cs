using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging and sorting of the years of the tracks.
/// </summary>
/// <param name="Offset">Number of years to skip.</param>
/// <param name="Limit">Maximum number of years of the page.</param>
/// <param name="Direction">Sort direction of the years.</param>
/// <param name="UserId">The user to whom <paramref name="MyMusic"/> refers.</param>
/// <param name="MyMusic">Only the tracks of the "my music" of the user count. Needs <paramref name="UserId"/>.</param>
public sealed record YearListQuery(
    int Offset,
    int Limit,
    SortDirection Direction,
    Guid? UserId = null,
    bool MyMusic = false);
