namespace WaveMP.Core.Streaming.Histories;

/// <summary>
/// Result of reporting a play.
/// </summary>
public enum PlayHistoryStatus
{
    Created,

    /// <summary>
    /// The track does not exist.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The start of the play lies in the future or too far in the past.
    /// </summary>
    InvalidPlayedAt,
}
