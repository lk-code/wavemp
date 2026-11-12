namespace WaveMP.Core.Transcoding.Services;

/// <summary>
/// Whether a track has an HLS variant.
/// </summary>
public enum TranscodeVariantStatus
{
    Found,

    /// <summary>
    /// The track does not exist.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The track has no file with the media (no sound or no video).
    /// </summary>
    NoMedia,

    /// <summary>
    /// The file of the track is gone since the last scan.
    /// </summary>
    FileMissing,

    /// <summary>
    /// The duration of the file is unknown, so no playlist can be computed.
    /// </summary>
    DurationUnknown,
}
