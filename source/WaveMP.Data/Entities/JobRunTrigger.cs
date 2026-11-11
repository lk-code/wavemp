namespace WaveMP.Data.Entities;

/// <summary>
/// What started a job run. Stored as its name.
/// </summary>
public enum JobRunTrigger
{
    /// <summary>
    /// The periodic schedule of the job.
    /// </summary>
    Schedule,

    /// <summary>
    /// An admin, through the API.
    /// </summary>
    Manual,

    /// <summary>
    /// The file watcher after changes in the media folder.
    /// </summary>
    FileWatcher,

    /// <summary>
    /// A library scan that finished, e.g. to prepare the covers of the changed library.
    /// </summary>
    LibraryScan,
}
