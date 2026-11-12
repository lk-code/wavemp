using System.Diagnostics.CodeAnalysis;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Services;

/// <summary>
/// Result of changing the transcoding settings: the saved settings or the errors why nothing was saved.
/// </summary>
public sealed record TranscodingSettingsResult(
    TranscodingSettings? Settings,
    IReadOnlyList<TranscodingSettingsValidationError> Errors)
{
    [MemberNotNullWhen(true, nameof(Settings))]
    public bool IsSuccess => Settings is not null;

    public static TranscodingSettingsResult Success(TranscodingSettings settings)
    {
        return new TranscodingSettingsResult(settings, []);
    }

    public static TranscodingSettingsResult Failure(IReadOnlyList<TranscodingSettingsValidationError> errors)
    {
        return new TranscodingSettingsResult(null, errors);
    }
}
