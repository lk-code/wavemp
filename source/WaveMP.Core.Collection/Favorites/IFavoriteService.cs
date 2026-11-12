using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Collection.Favorites;

/// <summary>
/// The favorite tracks, albums and artists of the profiles.
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// Marks a track, album or artist as favorite of the profile. Marking it again keeps the first time.
    /// </summary>
    Task<FavoriteStatus> AddAsync(
        Guid userId,
        FavoriteType type,
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the favorite of the profile; nothing happens if there is none.
    /// </summary>
    Task RemoveAsync(Guid userId, FavoriteType type, Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the favorites of the profile with their tracks, albums and artists, the newest first.
    /// </summary>
    Task<Page<FavoriteItem>> ListAsync(FavoriteListRequest request, CancellationToken cancellationToken = default);
}
