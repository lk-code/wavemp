namespace WaveMP.Core.Scanning.Edits;

/// <summary>
/// Why the metadata editor wrote nothing.
/// </summary>
public enum TrackMetadataError
{
    /// <summary>
    /// The track does not exist.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The track has no audio file.
    /// </summary>
    NoAudio,

    /// <summary>
    /// The audio file cannot be written, or writing its tags failed.
    /// </summary>
    NotWritable,
}
