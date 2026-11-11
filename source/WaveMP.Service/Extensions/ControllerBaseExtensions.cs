using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Library.Covers;
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
    /// The picture of a cover, or 404 with <paramref name="ownerNotFound"/> if the track or album does not exist and
    /// <c>cover.not_found</c> if it has no cover. The stream of the picture is disposed after the response.
    /// </summary>
    public static IActionResult Cover(this ControllerBase controller, CoverResult result, ProblemCodeDto ownerNotFound)
    {
        return result switch
        {
            { Status: CoverStatus.Found, Image: { } image } => controller.File(image.Content, image.ContentType),
            { Status: CoverStatus.OwnerNotFound } => controller.Problem(ownerNotFound, StatusCodes.Status404NotFound),
            _ => controller.Problem(ProblemCodeDto.CoverNotFound, StatusCodes.Status404NotFound),
        };
    }

    /// <summary>
    /// Checks an enum of the query. Model binding of query parameters also accepts numbers for enums, including
    /// undefined ones; such values are added to the model state under the name of the parameter.
    /// </summary>
    public static void ValidateEnum<TEnum>(this ControllerBase controller, TEnum? value, string name)
        where TEnum : struct, Enum
    {
        if (value is { } defined && !Enum.IsDefined(defined))
        {
            controller.ModelState.AddModelError(name, "The value is not valid.");
        }
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
