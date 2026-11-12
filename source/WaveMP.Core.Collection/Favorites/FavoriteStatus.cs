namespace WaveMP.Core.Collection.Favorites;

/// <summary>
/// Outcome of marking a favorite.
/// </summary>
public enum FavoriteStatus
{
    /// <summary>
    /// The target is a favorite of the profile, also if it was one before.
    /// </summary>
    Done,

    /// <summary>
    /// The track, album or artist does not exist.
    /// </summary>
    NotFound,
}
