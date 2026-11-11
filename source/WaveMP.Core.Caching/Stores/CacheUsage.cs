namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// The space the cache uses, in total and per area.
/// </summary>
/// <param name="IsAvailable">Whether the cache folder can be written; otherwise nothing is cached.</param>
/// <param name="SizeBytes">Size of all files in bytes.</param>
/// <param name="EntryCount">Number of all files.</param>
/// <param name="MaxSizeBytes">Size limit of the settings in bytes.</param>
/// <param name="Covers">Scaled covers.</param>
/// <param name="Transcodes">Transcoded media.</param>
public sealed record CacheUsage(
    bool IsAvailable,
    long SizeBytes,
    int EntryCount,
    long MaxSizeBytes,
    CacheAreaUsage Covers,
    CacheAreaUsage Transcodes);
