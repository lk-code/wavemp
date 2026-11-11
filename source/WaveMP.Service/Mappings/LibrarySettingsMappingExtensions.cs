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
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field."),
        };
    }

    /// <summary>
    /// Internal English description of a validation error; clients show their own text per field.
    /// </summary>
    public static string ToMessage(this SettingsErrorReason reason)
    {
        return reason switch
        {
            SettingsErrorReason.OutOfRange => "The value is out of range.",
            SettingsErrorReason.Invalid => "An entry of the list is not valid.",
            SettingsErrorReason.Overlap => "An extension is already in another list.",
            SettingsErrorReason.TooMany => "The list has too many entries.",
            SettingsErrorReason.Empty => "Audio and video extensions must not both be empty.",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown reason."),
        };
    }
}
