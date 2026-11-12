using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the settings of the transcoding. Missing or null values stay unchanged. Running transcodes keep their
/// settings; a change of the hardware acceleration starts a new detection.
/// </summary>
public sealed class UpdateTranscodingSettingsDto
{
    /// <summary>
    /// Whether Intel Quick Sync is detected and used for videos.
    /// </summary>
    public HardwareAccelerationModeDto? HardwareAcceleration { get; init; }

    /// <summary>
    /// How many ffmpeg processes transcode at the same time.
    /// </summary>
    [Range(1, 8)]
    public int? MaxParallelJobs { get; init; }

    /// <summary>
    /// Largest height of transcoded videos in pixels: 480, 720, 1080, 1440 or 2160.
    /// </summary>
    public int? MaxVideoHeight { get; init; }

    /// <summary>
    /// Bitrate of transcoded audio (AAC) in kbit/s: 128, 160, 192, 256 or 320.
    /// </summary>
    public int? AudioBitrateKbps { get; init; }
}
