using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// State of a job run.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<JobRunStatusDto>))]
public enum JobRunStatusDto
{
    /// <summary>
    /// Requested and waiting to start.
    /// </summary>
    Queued,

    Running,

    Succeeded,

    /// <summary>
    /// Ended with an error, see errorCode.
    /// </summary>
    Failed,

    /// <summary>
    /// Stopped before it ended, e.g. because the service shut down.
    /// </summary>
    Canceled,

    /// <summary>
    /// The service stopped unexpectedly while the run was queued or running.
    /// </summary>
    Interrupted,
}
