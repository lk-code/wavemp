using WaveMP.Data.Entities;

namespace WaveMP.Data.Projections;

/// <summary>
/// A favorite of a user.
/// </summary>
/// <param name="Id">Id of the favorite.</param>
/// <param name="Type">What the favorite marks.</param>
/// <param name="ItemId">Id of the track, album or artist.</param>
/// <param name="CreatedAt">When the user marked it.</param>
public sealed record FavoriteRow(Guid Id, FavoriteType Type, Guid ItemId, DateTimeOffset CreatedAt);
