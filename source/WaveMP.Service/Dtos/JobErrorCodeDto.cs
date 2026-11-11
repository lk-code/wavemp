using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable reason why a job run failed. Clients translate it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<JobErrorCodeDto>))]
public enum JobErrorCodeDto
{
    /// <summary>
    /// An unexpected error; details are in the server log.
    /// </summary>
    [JsonStringEnumMemberName("job.failed")]
    JobFailed,

    /// <summary>
    /// The service stopped while the run was queued or running.
    /// </summary>
    [JsonStringEnumMemberName("job.interrupted")]
    JobInterrupted,

    /// <summary>
    /// The media folder does not exist.
    /// </summary>
    [JsonStringEnumMemberName("scan.media_folder_not_found")]
    ScanMediaFolderNotFound,

    /// <summary>
    /// The media folder cannot be read.
    /// </summary>
    [JsonStringEnumMemberName("scan.media_folder_unreadable")]
    ScanMediaFolderUnreadable,

    /// <summary>
    /// The media folder is empty although the library has files, e.g. because it is not mounted; nothing was removed.
    /// </summary>
    [JsonStringEnumMemberName("scan.media_folder_empty")]
    ScanMediaFolderEmpty,

    /// <summary>
    /// ffprobe cannot be started.
    /// </summary>
    [JsonStringEnumMemberName("scan.probe_unavailable")]
    ScanProbeUnavailable,

    /// <summary>
    /// ffmpeg cannot be started, so covers cannot be scaled.
    /// </summary>
    [JsonStringEnumMemberName("cache.ffmpeg_unavailable")]
    CacheFfmpegUnavailable,

    /// <summary>
    /// The cache folder under the data folder cannot be written.
    /// </summary>
    [JsonStringEnumMemberName("cache.folder_unwritable")]
    CacheFolderUnwritable,
}
