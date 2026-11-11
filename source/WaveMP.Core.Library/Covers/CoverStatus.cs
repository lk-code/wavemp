namespace WaveMP.Core.Library.Covers;

/// <summary>
/// Outcome of a request for a cover.
/// </summary>
public enum CoverStatus
{
    Found,

    /// <summary>
    /// The track or album does not exist.
    /// </summary>
    OwnerNotFound,

    /// <summary>
    /// The track or album exists, but none of its covers can be delivered.
    /// </summary>
    NotFound,
}
