namespace WaveMP.Data.Repositories;

/// <summary>
/// Outcome of a change of the entries of a playlist.
/// </summary>
public enum PlaylistEntryChangeStatus
{
    Done,

    /// <summary>
    /// The playlist does not exist or belongs to another user.
    /// </summary>
    PlaylistNotFound,

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
}
