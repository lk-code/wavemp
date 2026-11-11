namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// What a scan found and changed. The totals count the whole library after the scan.
/// </summary>
public sealed record ScanStatistics
{
    public int DirectoriesScanned { get; init; }

    public int DirectoriesChanged { get; init; }

    public int FilesFound { get; init; }

    public int FilesAdded { get; init; }

    public int FilesUpdated { get; init; }

    public int FilesRemoved { get; init; }

    public int FilesUnchanged { get; init; }

    /// <summary>
    /// Files read with a problem or skipped, including those beyond the listed issues.
    /// </summary>
    public int FilesFailed { get; init; }

    public int TracksAdded { get; init; }

    public int TracksUpdated { get; init; }

    public int TracksRemoved { get; init; }

    public int TracksTotal { get; init; }

    public int AlbumsTotal { get; init; }

    public int ArtistsTotal { get; init; }

    /// <summary>
    /// Only some folders were scanned (e.g. after changes reported by the file watcher).
    /// </summary>
    public bool IsPartial { get; init; }
}
