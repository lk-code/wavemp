namespace WaveMP.Data.Entities;

/// <summary>
/// Category of a media file, decided by its extension (see the library settings). Stored as its name.
/// </summary>
public enum MediaFileKind
{
    Audio,

    Video,

    /// <summary>
    /// A picture: the cover of a track, the cover of an album or a picture without a track.
    /// </summary>
    Image,
}
