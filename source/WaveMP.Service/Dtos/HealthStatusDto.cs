using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Aggregated health status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HealthStatusDto>))]
public enum HealthStatusDto
{
    Healthy,
    Degraded,
    Unhealthy,
}
