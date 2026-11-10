namespace WaveMP.Service.Dtos;

/// <summary>
/// Health of the WaveMP service.
/// </summary>
public sealed class HealthDto
{
    /// <summary>
    /// Aggregated status of all health checks.
    /// </summary>
    public required HealthStatusDto Status { get; init; }

    /// <summary>
    /// Version of the running WaveMP service.
    /// </summary>
    public required string Version { get; init; }
}
