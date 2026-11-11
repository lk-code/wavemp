namespace WaveMP.Core.Scanning.Options;

/// <summary>
/// Settings of the scan that are not meant for the user interface (configuration section <c>Scanning</c>, e.g.
/// <c>Scanning__StartupDelay</c>).
/// </summary>
public sealed class ScanOptions
{
    public const string SectionName = "Scanning";

    /// <summary>
    /// Time from the start of the service to the first periodic scan.
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The file watcher scans once no change came in for this long.
    /// </summary>
    public TimeSpan WatcherDebounce { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The file watcher scans at the latest this long after the first change, even while changes keep coming.
    /// </summary>
    public TimeSpan WatcherMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of files read at the same time.
    /// </summary>
    public int MetadataParallelism { get; set; } = Math.Clamp(Environment.ProcessorCount, 1, 4);

    /// <summary>
    /// Files with problems listed in the result of a scan; all of them are counted.
    /// </summary>
    public int MaxIssuesPerScan { get; set; } = 50;

    /// <summary>
    /// Longest relative path (UTF-8 bytes) of a file; longer paths are skipped with an issue.
    /// </summary>
    public int MaxPathBytes { get; set; } = 2000;
}
