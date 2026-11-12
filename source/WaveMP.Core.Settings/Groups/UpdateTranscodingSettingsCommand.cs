namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Changes the transcoding settings. Every <c>null</c> value keeps the current value.
/// </summary>
public sealed record UpdateTranscodingSettingsCommand(
    HardwareAccelerationMode? HardwareAcceleration = null,
    int? MaxParallelJobs = null,
    int? MaxVideoHeight = null,
    int? AudioBitrateKbps = null);
