namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// The fields of <see cref="Groups.TranscodingSettings"/>, to name the field of a validation error.
/// </summary>
public enum TranscodingSettingsField
{
    HardwareAcceleration,
    MaxParallelJobs,
    MaxVideoHeight,
    AudioBitrateKbps,
}
