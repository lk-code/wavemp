using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Problem with a file found by a scan. Clients translate it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ScanIssueCodeDto>))]
public enum ScanIssueCodeDto
{
    /// <summary>
    /// The file cannot be read at all.
    /// </summary>
    [JsonStringEnumMemberName("file.unreadable")]
    FileUnreadable,

    /// <summary>
    /// The tags of the file cannot be read.
    /// </summary>
    [JsonStringEnumMemberName("file.tags_unreadable")]
    FileTagsUnreadable,

    /// <summary>
    /// ffprobe cannot read the file.
    /// </summary>
    [JsonStringEnumMemberName("file.probe_failed")]
    FileProbeFailed,

    /// <summary>
    /// The path of the file is too long; the file was skipped.
    /// </summary>
    [JsonStringEnumMemberName("file.path_too_long")]
    FilePathTooLong,
}
