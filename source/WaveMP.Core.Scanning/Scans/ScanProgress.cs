namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// The progress of a running scan.
/// </summary>
public sealed record ScanProgress(
    ScanPhase Phase,
    int DirectoriesFound,
    int FilesFound,
    int DirectoriesToProcess,
    int DirectoriesProcessed,
    int FilesToRead,
    int FilesRead);
