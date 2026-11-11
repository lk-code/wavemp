using Microsoft.AspNetCore.Mvc.ModelBinding;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Extensions;

public static class ModelStateDictionaryExtensions
{
    /// <summary>
    /// Adds a validation error with its code and limits to a field (JSON name of a member or name of a query
    /// parameter). <c>ValidationProblem()</c> writes it as <see cref="ValidationErrorDto"/>.
    /// </summary>
    public static void AddValidationError(
        this ModelStateDictionary modelState,
        string field,
        ValidationErrorCodeDto code,
        int? min = null,
        int? max = null)
    {
        modelState.AddModelError(field, new ValidationFailure(code, min, max).ToMessage());
    }
}
