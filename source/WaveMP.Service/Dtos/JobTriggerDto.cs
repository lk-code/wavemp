using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// What started a job run.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<JobTriggerDto>))]
public enum JobTriggerDto
{
    /// <summary>
    /// The periodic schedule of the job.
    /// </summary>
    Schedule,

    /// <summary>
    /// An admin, through the API.
    /// </summary>
    Manual,

    /// <summary>
    /// The file watcher after changes in the media folder.
    /// </summary>
    FileWatcher,
}
