namespace WaveMP.Service.Queries;

/// <summary>
/// Paging of all collections: query parameters <c>offset</c> (from 0) and <c>limit</c> (1 to <see cref="MaxLimit"/>).
/// </summary>
public static class PageLimits
{
    public const int DefaultLimit = 50;

    public const int MaxLimit = 100;

    /// <summary>
    /// Default number of results of each group of the search.
    /// </summary>
    public const int SearchDefaultLimit = 10;

    /// <summary>
    /// Maximum number of results of each group of the search.
    /// </summary>
    public const int SearchMaxLimit = 50;
}
