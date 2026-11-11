namespace WaveMP.Core.Jobs.Definitions;

/// <summary>
/// Stable ids of all background jobs. They name the jobs in the API and in the run history.
/// </summary>
public static class JobIds
{
    /// <summary>
    /// Scans the media folder and updates the library.
    /// </summary>
    public const string LibraryScan = "library-scan";

    /// <summary>
    /// Prepares the scaled covers after a scan and removes those of changed or removed pictures from the cache.
    /// </summary>
    public const string CoverCache = "cover-cache";
}
