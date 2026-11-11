using WaveMP.Core.Playlists.Playlists;

namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// Outcome of uploading the own cover of a playlist.
/// </summary>
public enum PlaylistCoverStatus
{
    Saved,

    /// <summary>
    /// The playlist does not exist or belongs to another user.
    /// </summary>
    NotFound,

    /// <summary>
    /// The content is no JPEG, PNG or WebP picture.
    /// </summary>
    UnsupportedFormat,

    /// <summary>
    /// The picture is damaged or cannot be converted.
    /// </summary>
    Unreadable,

    /// <summary>
    /// Covers cannot be stored right now: ffmpeg is missing or the folder cannot be written.
    /// </summary>
    Unavailable,
}

/// <summary>
/// The playlist with its new cover, or why the upload failed.
/// </summary>
/// <param name="Status">Whether the cover was stored.</param>
/// <param name="Item">The playlist with its new cover; set if stored.</param>
public sealed record PlaylistCoverResult(PlaylistCoverStatus Status, PlaylistItem? Item);
