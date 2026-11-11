using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class CacheMappingExtensions
{
    public static CacheUsageDto ToDto(this CacheUsage usage)
    {
        return new CacheUsageDto
        {
            IsAvailable = usage.IsAvailable,
            SizeBytes = usage.SizeBytes,
            EntryCount = usage.EntryCount,
            MaxSizeBytes = usage.MaxSizeBytes,
            Covers = usage.Covers.ToDto(),
            Transcodes = usage.Transcodes.ToDto(),
        };
    }

    public static CacheAreaUsageDto ToDto(this CacheAreaUsage usage)
    {
        return new CacheAreaUsageDto { SizeBytes = usage.SizeBytes, EntryCount = usage.EntryCount };
    }

    public static CacheSettingsDto ToDto(this CacheSettings settings)
    {
        return new CacheSettingsDto { MaxSizeMegabytes = settings.MaxSizeMegabytes };
    }

    public static UpdateCacheSettingsCommand ToCommand(this UpdateCacheSettingsDto request)
    {
        return new UpdateCacheSettingsCommand(request.MaxSizeMegabytes);
    }

    /// <summary>
    /// The JSON name of the field in <see cref="UpdateCacheSettingsDto"/>, the key of its validation errors.
    /// </summary>
    public static string ToJsonName(this CacheSettingsField field)
    {
        return field switch
        {
            CacheSettingsField.MaxSizeMegabytes => "maxSizeMegabytes",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field."),
        };
    }
}
