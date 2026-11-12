namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// Whether a segment can be delivered.
/// </summary>
public enum SegmentStatus
{
    /// <summary>
    /// The segment is in the cache.
    /// </summary>
    Ready,

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
    /// The playlist belongs to an older version of the file or of the settings.
    /// </summary>
    VariantOutdated,

    /// <summary>
    /// The variant has no such segment.
    /// </summary>
    NotFound,

    /// <summary>
    /// All transcode slots are taken by transcodes in use; try again shortly.
    /// </summary>
    Busy,

    /// <summary>
    /// The segment was not ready in time; try again shortly.
    /// </summary>
    Timeout,

    /// <summary>
    /// ffmpeg failed, or the duration of the file is unknown.
    /// </summary>
    Failed,

    /// <summary>
    /// Nothing can be transcoded: ffmpeg cannot be started or the cache cannot be written.
    /// </summary>
    Unavailable,
}
