using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable reason why a track has to be transcoded. Clients translate it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<TranscodeReasonDto>))]
public enum TranscodeReasonDto
{
    /// <summary>
    /// The client does not support the format of the file.
    /// </summary>
    [JsonStringEnumMemberName("format_unsupported")]
    FormatUnsupported,

    /// <summary>
    /// The format of the file is none of the formats clients play directly, for example WMA or APE.
    /// </summary>
    [JsonStringEnumMemberName("format_unknown")]
    FormatUnknown,
}
