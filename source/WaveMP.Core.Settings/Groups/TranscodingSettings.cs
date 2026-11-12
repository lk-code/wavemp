using System.Text.Json.Serialization;

namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Settings of the transcoding to HLS. Stored as the settings group <see cref="Key"/>; values missing in the stored
/// JSON keep the defaults below. Technical values (ffmpeg, render device, segment length) are options of the
/// configuration section <c>Transcoding</c> instead.
/// </summary>
public sealed class TranscodingSettings
{
    /// <summary>
    /// Key of the settings group.
    /// </summary>
    public const string Key = "transcoding";

    /// <summary>
    /// Whether Intel Quick Sync is detected and used. Stored as its name.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<HardwareAccelerationMode>))]
    public HardwareAccelerationMode HardwareAcceleration { get; init; } = HardwareAccelerationMode.Auto;

    /// <summary>
    /// How many ffmpeg processes transcode at the same time.
    /// </summary>
    public int MaxParallelJobs { get; init; } = 2;

    /// <summary>
    /// Largest height of transcoded videos in pixels; larger videos are scaled down, smaller ones are never scaled up.
    /// </summary>
    public int MaxVideoHeight { get; init; } = 1080;

    /// <summary>
    /// Bitrate of transcoded audio (AAC) in kbit/s.
    /// </summary>
    public int AudioBitrateKbps { get; init; } = 256;
}
