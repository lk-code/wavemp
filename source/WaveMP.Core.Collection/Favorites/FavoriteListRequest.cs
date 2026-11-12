using WaveMP.Data.Entities;

namespace WaveMP.Core.Collection.Favorites;

/// <summary>
/// A request for a page of the favorites of a profile, the newest first.
/// </summary>
/// <param name="UserId">Id of the profile.</param>
/// <param name="Offset">Number of favorites to skip.</param>
/// <param name="Limit">Maximum number of favorites of the page.</param>
/// <param name="Type">Only favorites of this type.</param>
public sealed record FavoriteListRequest(Guid UserId, int Offset, int Limit, FavoriteType? Type = null);
