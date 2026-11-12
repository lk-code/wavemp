namespace WaveMP.Core.Library.Searches;

/// <summary>
/// Searches the whole library: artists, albums and tracks at once.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// The best matches of each group, the same page of each. Finds texts that contain the search text or are similar
    /// to it (small typos). Returns <c>null</c> for a text without content. The results carry the favorites and ratings
    /// of the profile <paramref name="userId"/>.
    /// </summary>
    Task<SearchResult?> SearchAsync(
        string? text,
        int offset,
        int limit,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
