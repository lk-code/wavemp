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

    /// <summary>
    /// Checks the enums of a query of job runs. Model binding of query parameters also accepts numbers for enums,
    /// including undefined ones; such values are added to the model state. Returns whether the query is valid.
    /// </summary>
    public static bool ValidateRunQuery(
        this ControllerBase controller,
        SortDirectionDto direction,
        JobRunStatusDto? status,
        JobTriggerDto? trigger)
    {
        if (!Enum.IsDefined(direction))
        {
            controller.ModelState.AddModelError(nameof(direction), "The sort direction is not valid.");
        }

        if (status is { } statusValue && !Enum.IsDefined(statusValue))
        {
            controller.ModelState.AddModelError(nameof(status), "The status is not valid.");
        }

        if (trigger is { } triggerValue && !Enum.IsDefined(triggerValue))
        {
            controller.ModelState.AddModelError(nameof(trigger), "The trigger is not valid.");
        }

        return controller.ModelState.IsValid;
    }
}
