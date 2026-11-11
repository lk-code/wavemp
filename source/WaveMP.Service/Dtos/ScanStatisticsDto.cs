namespace WaveMP.Service.Dtos;

/// <summary>
/// What a scan found and changed; the totals count the whole library after the scan.
/// </summary>
public sealed class ScanStatisticsDto
{
    public required int DirectoriesScanned { get; init; }

    public required int DirectoriesChanged { get; init; }

    public required int FilesFound { get; init; }

    public required int FilesAdded { get; init; }

    public required int FilesUpdated { get; init; }

    public required int FilesRemoved { get; init; }

    public required int FilesUnchanged { get; init; }

    /// <summary>
    /// Files with problems, including those beyond the listed issues.
    /// </summary>
    public required int FilesFailed { get; init; }

    public required int TracksAdded { get; init; }

    public required int TracksUpdated { get; init; }

    public required int TracksRemoved { get; init; }

    public required int TracksTotal { get; init; }

    public required int AlbumsTotal { get; init; }

    public required int ArtistsTotal { get; init; }
}
