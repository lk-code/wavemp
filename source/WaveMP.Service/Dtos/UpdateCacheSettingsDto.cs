using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the settings of the cache. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateCacheSettingsDto
{
    /// <summary>
    /// Largest size of the cache in megabytes (1 MB = 1,000,000 bytes); a lower limit cleans up the cache at once.
    /// </summary>
    [Range(100, 1_000_000)]
    public int? MaxSizeMegabytes { get; init; }
}
