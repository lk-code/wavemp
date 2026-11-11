using WaveMP.Service.Dtos;

namespace WaveMP.Service.Problems;

/// <summary>
/// Assigns the machine-readable <see cref="ProblemCodeDto"/> to Problem Details responses.
/// </summary>
public static class ProblemCodes
{
    /// <summary>
    /// Name of the Problem Details extension member that carries the code.
    /// </summary>
    public const string ExtensionName = "code";

    /// <summary>
    /// Returns the default code for a status code. Endpoints set a more specific code where one exists.
    /// </summary>
    public static ProblemCodeDto ForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => ProblemCodeDto.RequestInvalid,
            StatusCodes.Status404NotFound => ProblemCodeDto.RequestNotFound,
            StatusCodes.Status503ServiceUnavailable => ProblemCodeDto.ServiceUnavailable,
            < StatusCodes.Status500InternalServerError => ProblemCodeDto.RequestFailed,
            _ => ProblemCodeDto.ServerError,
        };
    }
}
