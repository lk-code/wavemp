namespace WaveMP.Data.Entities;

/// <summary>
/// Machine-readable reason why a job run failed. Stored as its name; clients translate it.
/// </summary>
public enum JobErrorCode
{
    /// <summary>
    /// An unexpected error; details are in the log.
    /// </summary>
    Failed,

    /// <summary>
    /// The service stopped while the run was queued or running.
    /// </summary>
    Interrupted,

    /// <summary>
    /// The media folder does not exist.
    /// </summary>
    MediaFolderNotFound,

    /// <summary>
    /// The media folder cannot be read.
    /// </summary>
    MediaFolderUnreadable,

    /// <summary>
    /// The media folder is empty although the library has files, e.g. because it is not mounted. Nothing was removed.
    /// </summary>
    MediaFolderEmpty,

    /// <summary>
    /// ffprobe cannot be started.
    /// </summary>
    ProbeUnavailable,
}
