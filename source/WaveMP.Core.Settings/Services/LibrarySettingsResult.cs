using System.Diagnostics.CodeAnalysis;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Services;

/// <summary>
/// Result of changing the library settings: the saved settings or the errors why nothing was saved.
/// </summary>
public sealed record LibrarySettingsResult(LibrarySettings? Settings, IReadOnlyList<SettingsValidationError> Errors)
{
    [MemberNotNullWhen(true, nameof(Settings))]
    public bool IsSuccess => Settings is not null;

    public static LibrarySettingsResult Success(LibrarySettings settings)
    {
        return new LibrarySettingsResult(settings, []);
    }

    public static LibrarySettingsResult Failure(IReadOnlyList<SettingsValidationError> errors)
    {
        return new LibrarySettingsResult(null, errors);
    }
}
