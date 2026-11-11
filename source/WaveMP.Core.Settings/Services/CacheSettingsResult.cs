using System.Diagnostics.CodeAnalysis;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Services;

/// <summary>
/// Result of changing the cache settings: the saved settings or the errors why nothing was saved.
/// </summary>
public sealed record CacheSettingsResult(CacheSettings? Settings, IReadOnlyList<CacheSettingsValidationError> Errors)
{
    [MemberNotNullWhen(true, nameof(Settings))]
    public bool IsSuccess => Settings is not null;

    public static CacheSettingsResult Success(CacheSettings settings)
    {
        return new CacheSettingsResult(settings, []);
    }

    public static CacheSettingsResult Failure(IReadOnlyList<CacheSettingsValidationError> errors)
    {
        return new CacheSettingsResult(null, errors);
    }
}
