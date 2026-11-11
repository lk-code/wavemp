namespace WaveMP.Data.Entities;

/// <summary>
/// Which cover a track shows: the cover of its album or its own. If the preferred cover is missing, the other one is
/// used. Stored as its name.
/// </summary>
public enum CoverPriority
{
    /// <summary>
    /// The cover of the album (a picture in its folder) before the cover of the track.
    /// </summary>
    AlbumFirst,

    /// <summary>
    /// The cover of the track (a picture with its base name or embedded in its files) before the cover of the album.
    /// </summary>
    TrackFirst,
}
