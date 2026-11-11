using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Id of a background job.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<JobIdDto>))]
public enum JobIdDto
{
    /// <summary>
    /// Scans the media folder and updates the library.
    /// </summary>
    [JsonStringEnumMemberName("library-scan")]
    LibraryScan,

    /// <summary>
    /// Prepares the scaled covers after scans and removes those of changed or removed pictures.
    /// </summary>
    [JsonStringEnumMemberName("cover-cache")]
    CoverCache,
}
