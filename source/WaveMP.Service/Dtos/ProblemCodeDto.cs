using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable code of a problem, part of every Problem Details response. Clients translate it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProblemCodeDto>))]
public enum ProblemCodeDto
{
    /// <summary>
    /// The request is malformed or failed validation (400).
    /// </summary>
    [JsonStringEnumMemberName("request.invalid")]
    RequestInvalid,

    /// <summary>
    /// The requested resource or API route does not exist (404).
    /// </summary>
    [JsonStringEnumMemberName("request.not_found")]
    RequestNotFound,

    /// <summary>
    /// Any other client error without a more specific code (4xx).
    /// </summary>
    [JsonStringEnumMemberName("request.failed")]
    RequestFailed,

    /// <summary>
    /// An unexpected error occurred on the server (500 and any other 5xx without a more specific code).
    /// </summary>
    [JsonStringEnumMemberName("server.error")]
    ServerError,

    /// <summary>
    /// The service is temporarily unavailable (503).
    /// </summary>
    [JsonStringEnumMemberName("service.unavailable")]
    ServiceUnavailable,

    /// <summary>
    /// At least one health check failed, e.g. the database is unreachable (503).
    /// </summary>
    [JsonStringEnumMemberName("health.unhealthy")]
    HealthUnhealthy,

    /// <summary>
    /// No valid session token, or the session has ended or expired (401).
    /// </summary>
    [JsonStringEnumMemberName("auth.unauthorized")]
    AuthUnauthorized,

    /// <summary>
    /// The profile is not allowed to do this, e.g. an endpoint for admins only (403).
    /// </summary>
    [JsonStringEnumMemberName("auth.forbidden")]
    AuthForbidden,
}
