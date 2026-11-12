using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable reason why the hardware is not used. Clients translate it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<HardwareUnavailableReasonDto>))]
public enum HardwareUnavailableReasonDto
{
    /// <summary>
    /// ffmpeg cannot be started; nothing can be transcoded.
    /// </summary>
    [JsonStringEnumMemberName("ffmpeg_missing")]
    FfmpegMissing,

    /// <summary>
    /// No render device (<c>/dev/dri/renderD*</c>) is passed into the container.
    /// </summary>
    [JsonStringEnumMemberName("no_device")]
    NoDevice,

    /// <summary>
    /// The test encodes with Quick Sync and VA-API failed, e.g. missing driver or permissions.
    /// </summary>
    [JsonStringEnumMemberName("test_failed")]
    TestFailed,

    /// <summary>
    /// A transcode with the hardware failed.
    /// </summary>
    [JsonStringEnumMemberName("encode_failed")]
    EncodeFailed,
}
