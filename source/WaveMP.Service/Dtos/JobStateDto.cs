using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// What a job is doing right now.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<JobStateDto>))]
public enum JobStateDto
{
    Idle,

    /// <summary>
    /// A run was requested and waits to start.
    /// </summary>
    Queued,

    Running,
}
