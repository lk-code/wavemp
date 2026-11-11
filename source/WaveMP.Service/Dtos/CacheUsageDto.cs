namespace WaveMP.Service.Dtos;

/// <summary>
/// The space the cache uses, in total and per area, and its size limit.
/// </summary>
public sealed class CacheUsageDto
{
    /// <summary>
    /// Whether the cache folder can be written. Without it nothing is cached and covers are delivered unscaled.
    /// </summary>
    public required bool IsAvailable { get; init; }

    /// <summary>
    /// Size of all files of the cache in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Number of all files of the cache.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Size limit of the cache settings in bytes; beyond it the files used least recently are removed.
    /// </summary>
    public required long MaxSizeBytes { get; init; }

    /// <summary>
    /// Scaled covers.
    /// </summary>
    public required CacheAreaUsageDto Covers { get; init; }

    /// <summary>
    /// Transcoded media.
    /// </summary>
    public required CacheAreaUsageDto Transcodes { get; init; }
}
