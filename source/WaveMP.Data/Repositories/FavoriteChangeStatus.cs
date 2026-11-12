namespace WaveMP.Data.Repositories;

/// <summary>
/// Outcome of marking a favorite.
/// </summary>
public enum FavoriteChangeStatus
{
    Added,

    /// <summary>
    /// The user had already marked the target; the favorite keeps its time.
    /// </summary>
    Existing,

    /// <summary>
    /// The track, album or artist does not exist; nothing was changed.
    /// </summary>
    TargetNotFound,
}
