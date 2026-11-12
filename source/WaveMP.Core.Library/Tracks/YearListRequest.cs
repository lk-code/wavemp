using WaveMP.Data.Queries;

namespace WaveMP.Core.Library.Tracks;

/// <summary>
/// A request for a page of the years of the tracks.
/// </summary>
/// <param name="Offset">Number of years to skip.</param>
/// <param name="Limit">Maximum number of years of the page.</param>
/// <param name="Direction">Sort direction; the newest year first by default.</param>
/// <param name="UserId">The profile to whom <paramref name="MyMusic"/> refers.</param>
/// <param name="MyMusic">Only the tracks of the "my music" of the profile count; ignored without a profile.</param>
public sealed record YearListRequest(
    int Offset,
    int Limit,
    SortDirection? Direction = null,
    Guid? UserId = null,
    bool MyMusic = false);
