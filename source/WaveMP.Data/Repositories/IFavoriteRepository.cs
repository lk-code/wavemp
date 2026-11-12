using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// The favorite tracks, albums and artists of the users. Every access names the user. Nothing is tracked.
/// </summary>
public interface IFavoriteRepository
{
    /// <summary>
    /// Marks the target of the favorite (its type and the id of the type) for its user; a target the user already
    /// marked keeps its favorite.
    /// </summary>
    Task<FavoriteChangeStatus> AddAsync(Favorite favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the favorite of the user; returns <c>false</c> if there was none.
    /// </summary>
    Task<bool> RemoveAsync(
        Guid userId,
        FavoriteType type,
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the favorites of the user, the newest first (ties by id, descending).
    /// </summary>
    Task<Page<FavoriteRow>> ListAsync(FavoriteListQuery query, CancellationToken cancellationToken = default);
}
