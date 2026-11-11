namespace WaveMP.Service.Dtos;

/// <summary>
/// Progress of a running scan.
/// </summary>
public sealed class ScanProgressDto
{
    public required ScanPhaseDto Phase { get; init; }

    /// <summary>
    /// Folders found so far.
    /// </summary>
    public required int DirectoriesFound { get; init; }

    /// <summary>
    /// Media files found so far.
    /// </summary>
    public required int FilesFound { get; init; }

    /// <summary>
    /// Folders with changes that are updated.
    /// </summary>
    public required int DirectoriesToProcess { get; init; }

    public required int DirectoriesProcessed { get; init; }

    /// <summary>
    /// New and changed files that are read.
    /// </summary>
    public required int FilesToRead { get; init; }

    public required int FilesRead { get; init; }
}
