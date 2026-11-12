using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Whether Intel Quick Sync is detected and used for transcoding videos.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<HardwareAccelerationModeDto>))]
public enum HardwareAccelerationModeDto
{
    /// <summary>
    /// Detected at the start and used if the test encode works; otherwise software.
    /// </summary>
    Auto,

    /// <summary>
    /// Always software.
    /// </summary>
    Off,
}
