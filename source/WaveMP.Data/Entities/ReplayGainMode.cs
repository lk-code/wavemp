namespace WaveMP.Data.Entities;

/// <summary>
/// How the player normalizes the volume with the ReplayGain tags of the files. Stored as its name.
/// </summary>
public enum ReplayGainMode
{
    /// <summary>
    /// No normalization; the default.
    /// </summary>
    Off,

    /// <summary>
    /// The gain of the track: every track plays at the same loudness.
    /// </summary>
    Track,

    /// <summary>
    /// The gain of the album, so the differences between the tracks of an album stay; the gain of the track if the
    /// file has no album gain.
    /// </summary>
    Album,
}
