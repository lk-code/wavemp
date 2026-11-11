using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// What a running scan is doing.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ScanPhaseDto>))]
public enum ScanPhaseDto
{
    /// <summary>
    /// Walks the media folder and compares it with the library.
    /// </summary>
    Discovering,

    /// <summary>
    /// Reads new and changed files and updates their folders.
    /// </summary>
    Processing,

    /// <summary>
    /// Removes what is gone and updates album covers and totals.
    /// </summary>
    CleaningUp,
}
