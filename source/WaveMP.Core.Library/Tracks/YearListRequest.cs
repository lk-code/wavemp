using WaveMP.Data.Queries;

namespace WaveMP.Core.Library.Tracks;

/// <summary>
/// A request for a page of the years of the tracks.
/// </summary>
/// <param name="Offset">Number of years to skip.</param>
/// <param name="Limit">Maximum number of years of the page.</param>
/// <param name="Direction">Sort direction; the newest year first by default.</param>
public sealed record YearListRequest(int Offset, int Limit, SortDirection? Direction = null);
