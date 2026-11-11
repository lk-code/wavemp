using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Reports whether the service and its database are ready.
/// </summary>
[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    private static readonly string _serviceVersion = GetServiceVersion();

    /// <summary>
    /// Returns the health of the service.
    /// </summary>
    /// <response code="200">The service is healthy or degraded.</response>
    /// <response code="503">At least one health check failed (code <c>health.unhealthy</c>).</response>
    [HttpGet(Name = "GetHealth")]
    [ProducesResponseType<HealthDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<HealthDto>> GetHealthAsync(CancellationToken cancellationToken)
    {
        HealthReport report = await healthCheckService.CheckHealthAsync(cancellationToken);

        if (report.Status == HealthStatus.Unhealthy)
        {
            return this.Problem(ProblemCodeDto.HealthUnhealthy, StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(new HealthDto
        {
            Status = report.Status == HealthStatus.Degraded ? HealthStatusDto.Degraded : HealthStatusDto.Healthy,
            Version = _serviceVersion,
        });
    }

    private static string GetServiceVersion()
    {
        string? version = typeof(HealthController).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        // Strip the source revision ("+<commit>") that the SDK appends.
        return version?.Split('+')[0] ?? "unknown";
    }
}
