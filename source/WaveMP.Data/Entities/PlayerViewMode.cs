namespace WaveMP.Data.Entities;

/// <summary>
/// What the Now Playing stage of the player shows. Stored as its name.
/// </summary>
public enum PlayerViewMode
{
    /// <summary>
    /// The cover of the track.
    /// </summary>
    Cover,

    /// <summary>
    /// The video of tracks with a video, the cover of the others; the default.
    /// </summary>
    Video,

    /// <summary>
    /// A visualization of the music.
    /// </summary>
    Visualization,
}
