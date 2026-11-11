using Microsoft.AspNetCore.Mvc;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Extensions;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// Creates a Problem Details response with an explicit <see cref="ProblemCodeDto"/> instead of the default code
    /// of the status code. Title and detail keep the framework defaults; clients only evaluate the code.
    /// </summary>
    public static ObjectResult Problem(this ControllerBase controller, ProblemCodeDto code, int statusCode)
    {
        ObjectResult result = controller.Problem(statusCode: statusCode);
        if (result.Value is ProblemDetails problem)
        {
            problem.Extensions[ProblemCodes.ExtensionName] = code;
        }

        return result;
    }
}
