namespace WaveMP.Service.Dtos;

/// <summary>
/// Settings of the transcoding.
/// </summary>
public sealed class TranscodingSettingsDto
{
    /// <summary>
    /// Whether Intel Quick Sync is detected and used for videos.
    /// </summary>
    public required HardwareAccelerationModeDto HardwareAcceleration { get; init; }

    /// <summary>
    /// How many ffmpeg processes transcode at the same time (1 to 8).
    /// </summary>
    public required int MaxParallelJobs { get; init; }

    /// <summary>
    /// Largest height of transcoded videos in pixels (480, 720, 1080, 1440 or 2160); larger videos are scaled
    /// down, smaller ones are never scaled up.
    /// </summary>
    public required int MaxVideoHeight { get; init; }

    /// <summary>
    /// Bitrate of transcoded audio (AAC) in kbit/s (128, 160, 192, 256 or 320).
    /// </summary>
    public required int AudioBitrateKbps { get; init; }
}
