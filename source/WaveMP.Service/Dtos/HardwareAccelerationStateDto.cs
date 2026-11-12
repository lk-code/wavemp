using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// State of the hardware acceleration of the transcoding.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<HardwareAccelerationStateDto>))]
public enum HardwareAccelerationStateDto
{
    /// <summary>
    /// The detection runs; new transcodes use software meanwhile.
    /// </summary>
    Detecting,

    /// <summary>
    /// New transcodes of videos use the hardware.
    /// </summary>
    Active,

    /// <summary>
    /// No usable hardware (see <c>reason</c>); software is used.
    /// </summary>
    Unavailable,

    /// <summary>
    /// Turned off in the settings; software is used.
    /// </summary>
    Disabled,

    /// <summary>
    /// A transcode with the hardware failed; software is used until the next detection.
    /// </summary>
    Failed,
}
