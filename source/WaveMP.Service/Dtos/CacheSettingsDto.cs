namespace WaveMP.Service.Dtos;

/// <summary>
/// Settings of the cache.
/// </summary>
public sealed class CacheSettingsDto
{
    /// <summary>
    /// Largest size of the cache in megabytes (1 MB = 1,000,000 bytes; 100 to 1,000,000). When the cache grows larger,
    /// the files used least recently are removed.
    /// </summary>
    public required int MaxSizeMegabytes { get; init; }
}
