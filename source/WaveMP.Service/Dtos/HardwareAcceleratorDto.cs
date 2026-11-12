using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// An encoder of the graphics hardware.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<HardwareAcceleratorDto>))]
public enum HardwareAcceleratorDto
{
    /// <summary>
    /// Software (libx264).
    /// </summary>
    None,

    /// <summary>
    /// VA-API (h264_vaapi).
    /// </summary>
    Vaapi,

    /// <summary>
    /// Intel Quick Sync Video (h264_qsv).
    /// </summary>
    Qsv,
}
