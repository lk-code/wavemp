namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// What a playlist shows as its cover.
/// </summary>
public enum PlaylistCoverKind
{
    /// <summary>
    /// The cover uploaded for the playlist.
    /// </summary>
    Custom,

    /// <summary>
    /// The covers of up to four of its tracks.
    /// </summary>
    Tracks,

    /// <summary>
    /// No cover: the playlist has neither an own cover nor tracks with a cover.
    /// </summary>
    None,
}
