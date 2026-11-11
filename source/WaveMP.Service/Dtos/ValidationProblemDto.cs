using Microsoft.AspNetCore.Mvc;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Problem Details (RFC 9457) of a request that failed validation (400, code <c>request.invalid</c>). Instead of texts,
/// every error names its field and a machine-readable code with its limits; clients translate them.
/// </summary>
public sealed class ValidationProblemDto : ProblemDetails
{
    /// <summary>
    /// The errors, at least one; a field can have more than one.
    /// </summary>
    public required IReadOnlyList<ValidationErrorDto> Errors { get; init; }
}
