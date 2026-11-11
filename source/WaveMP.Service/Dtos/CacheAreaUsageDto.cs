namespace WaveMP.Service.Dtos;

/// <summary>
/// The space an area of the cache uses.
/// </summary>
public sealed class CacheAreaUsageDto
{
    /// <summary>
    /// Size of its files in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Number of its files.
    /// </summary>
    public required int EntryCount { get; init; }
}
