using WaveMP.Core.Settings.Groups;

namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// Checks the transcoding settings before they are saved.
/// </summary>
public static class TranscodingSettingsValidator
{
    public const int MinMaxParallelJobs = 1;
    public const int MaxMaxParallelJobs = 8;

    /// <summary>
    /// The heights a transcoded video may be limited to (480p to 2160p).
    /// </summary>
    public static IReadOnlyList<int> VideoHeights { get; } = [480, 720, 1080, 1440, 2160];

    /// <summary>
    /// The bitrates of transcoded audio in kbit/s.
    /// </summary>
    public static IReadOnlyList<int> AudioBitrates { get; } = [128, 160, 192, 256, 320];

    /// <summary>
    /// All errors of the settings; an empty list means they are valid.
    /// </summary>
    public static IReadOnlyList<TranscodingSettingsValidationError> Validate(TranscodingSettings settings)
    {
        List<TranscodingSettingsValidationError> errors = [];

        // The stored JSON may hold a number instead of the name.
        if (!Enum.IsDefined(settings.HardwareAcceleration))
        {
            errors.Add(new(TranscodingSettingsField.HardwareAcceleration, SettingsErrorReason.Invalid));
        }

        if (settings.MaxParallelJobs is < MinMaxParallelJobs or > MaxMaxParallelJobs)
        {
            errors.Add(new(
                TranscodingSettingsField.MaxParallelJobs,
                SettingsErrorReason.OutOfRange,
                MinMaxParallelJobs,
                MaxMaxParallelJobs));
        }

        if (!VideoHeights.Contains(settings.MaxVideoHeight))
        {
            errors.Add(new(TranscodingSettingsField.MaxVideoHeight, SettingsErrorReason.Invalid));
        }

        if (!AudioBitrates.Contains(settings.AudioBitrateKbps))
        {
            errors.Add(new(TranscodingSettingsField.AudioBitrateKbps, SettingsErrorReason.Invalid));
        }

        return errors;
    }
}
