namespace WaveMP.Core.Collection.HomePages;

/// <summary>
/// The home page of a profile: its music at a glance.
/// </summary>
public interface IHomePageService
{
    /// <summary>
    /// All rows of the home page, each with at most <paramref name="limit"/> items.
    /// </summary>
    Task<HomePage> GetAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
}
