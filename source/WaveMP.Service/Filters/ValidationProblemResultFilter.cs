using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Filters;

/// <summary>
/// Replaces every <see cref="ValidationProblemDetails"/> of the framework, e.g. from <c>ValidationProblem()</c> in a
/// controller, with the <see cref="Dtos.ValidationProblemDto"/> of the model state, so no response carries the English
/// texts of ASP.NET Core. The automatic responses of <c>[ApiController]</c> are created as
/// <see cref="Dtos.ValidationProblemDto"/> right away (<see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory"/>).
/// </summary>
public sealed class ValidationProblemResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult { Value: ValidationProblemDetails })
        {
            context.Result = ValidationProblems.CreateResponse(context);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
