using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Aggregated health status.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<HealthStatusDto>))]
public enum HealthStatusDto
{
    Healthy,
    Degraded,
    Unhealthy,
}
