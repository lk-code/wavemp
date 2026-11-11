using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class LibrarySettingsMappingExtensions
{
    public static LibrarySettingsDto ToDto(this LibrarySettings settings)
    {
        return new LibrarySettingsDto
        {
            ScanIntervalMinutes = settings.ScanIntervalMinutes,
            FileWatcherEnabled = settings.FileWatcherEnabled,
            AudioExtensions = settings.AudioExtensions,
            VideoExtensions = settings.VideoExtensions,
            ImageExtensions = settings.ImageExtensions,
            AlbumCoverNames = settings.AlbumCoverNames,
        };
    }

    public static UpdateLibrarySettingsCommand ToCommand(this UpdateLibrarySettingsDto request)
    {
        return new UpdateLibrarySettingsCommand(
            request.ScanIntervalMinutes,
            request.FileWatcherEnabled,
            request.AudioExtensions,
            request.VideoExtensions,
            request.ImageExtensions,
            request.AlbumCoverNames);
    }

    /// <summary>
    /// The JSON name of the field in <see cref="UpdateLibrarySettingsDto"/>, the key of its validation errors.
    /// </summary>
    public static string ToJsonName(this LibrarySettingsField field)
    {
        return field switch
        {
            LibrarySettingsField.ScanIntervalMinutes => "scanIntervalMinutes",
            LibrarySettingsField.FileWatcherEnabled => "fileWatcherEnabled",
            LibrarySettingsField.AudioExtensions => "audioExtensions",
            LibrarySettingsField.VideoExtensions => "videoExtensions",
            LibrarySettingsField.ImageExtensions => "imageExtensions",
            LibrarySettingsField.AlbumCoverNames => "albumCoverNames",
            LibrarySettingsField.CoverPriority => "coverPriority",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field."),
        };
    }

    /// <summary>
    /// The code of a validation error of the settings; clients translate it.
    /// </summary>
    public static ValidationErrorCodeDto ToCode(this SettingsErrorReason reason)
    {
        return reason switch
        {
            SettingsErrorReason.OutOfRange => ValidationErrorCodeDto.OutOfRange,
            SettingsErrorReason.Invalid => ValidationErrorCodeDto.InvalidFormat,
            SettingsErrorReason.Overlap => ValidationErrorCodeDto.Duplicate,
            SettingsErrorReason.TooMany => ValidationErrorCodeDto.MaxItems,
            // Audio and video extensions must not both be empty.
            SettingsErrorReason.Empty => ValidationErrorCodeDto.Required,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown reason."),
        };
    }
}
