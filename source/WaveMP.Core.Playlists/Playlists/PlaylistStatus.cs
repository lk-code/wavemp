namespace WaveMP.Core.Playlists.Playlists;

/// <summary>
/// Outcome of a request for a playlist or its entries.
/// </summary>
public enum PlaylistStatus
{
    Ok,

    /// <summary>
    /// The playlist does not exist or belongs to another user.
    /// </summary>
    NotFound,

    /// <summary>
    /// The entry does not exist in the playlist.
    /// </summary>
    EntryNotFound,

    /// <summary>
    /// At least one of the tracks does not exist; nothing was changed.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The playlist would hold more entries than allowed; nothing was changed.
    /// </summary>
    Full,

    /// <summary>
    /// The position lies outside the playlist; nothing was changed.
    /// </summary>
    InvalidPosition,

    /// <summary>
    /// The name is empty after normalization, too long or contains control characters.
    /// </summary>
    InvalidName,
}
