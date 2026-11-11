using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Problems;

/// <summary>
/// Writes validation problems as <see cref="ValidationProblemDto"/>: one code per error instead of the English texts of
/// ASP.NET Core. Creates the automatic 400 responses of <c>[ApiController]</c>
/// (<see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory"/>) and replaces the result of
/// <c>ValidationProblem()</c> in the controllers (<see cref="Filters.ValidationProblemResultFilter"/>).
/// </summary>
public static class ValidationProblems
{
    /// <summary>
    /// Title of every validation problem, the one the framework uses as well; clients only evaluate the codes.
    /// </summary>
    public const string Title = "One or more validation errors occurred.";

    private const string ProblemContentType = "application/problem+json";
    private const string JsonPathRoot = "$";

    /// <summary>
    /// Replaces the texts of model binding (missing or unreadable values) with internal messages of codes.
    /// </summary>
    public static void UseCodes(DefaultModelBindingMessageProvider messages)
    {
        string required = new ValidationFailure(ValidationErrorCodeDto.Required).ToMessage();
        string invalidFormat = new ValidationFailure(ValidationErrorCodeDto.InvalidFormat).ToMessage();

        messages.SetMissingBindRequiredValueAccessor(_ => required);
        messages.SetMissingKeyOrValueAccessor(() => required);
        messages.SetMissingRequestBodyRequiredValueAccessor(() => required);
        messages.SetValueMustNotBeNullAccessor(_ => required);
        messages.SetAttemptedValueIsInvalidAccessor((_, _) => invalidFormat);
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => invalidFormat);
        messages.SetUnknownValueIsInvalidAccessor(_ => invalidFormat);
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => invalidFormat);
        messages.SetValueIsInvalidAccessor(_ => invalidFormat);
        messages.SetValueMustBeANumberAccessor(_ => invalidFormat);
        messages.SetNonPropertyValueMustBeANumberAccessor(() => invalidFormat);
    }

    /// <summary>
    /// The response for an invalid model state: 400 with a <see cref="ValidationProblemDto"/>.
    /// </summary>
    public static IActionResult CreateResponse(ActionContext context)
    {
        ProblemDetailsFactory factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        ProblemDetails defaults = factory.CreateProblemDetails(
            context.HttpContext,
            StatusCodes.Status400BadRequest,
            title: Title);

        ValidationProblemDto problem = new()
        {
            Type = defaults.Type,
            Title = defaults.Title,
            Status = defaults.Status,
            Detail = defaults.Detail,
            Instance = defaults.Instance,
            Errors = ToErrors(context),
        };
        foreach (KeyValuePair<string, object?> extension in defaults.Extensions)
        {
            problem.Extensions[extension.Key] = extension.Value;
        }

        problem.Extensions[ProblemCodes.ExtensionName] = ProblemCodeDto.RequestInvalid;

        BadRequestObjectResult result = new(problem);
        result.ContentTypes.Add(ProblemContentType);
        return result;
    }

    /// <summary>
    /// The errors of the model state with their fields. Texts that are no internal message of a code (e.g. the
    /// exception of malformed JSON) become <see cref="ValidationErrorCodeDto.InvalidFormat"/>.
    /// </summary>
    public static IReadOnlyList<ValidationErrorDto> ToErrors(ActionContext context)
    {
        HashSet<string> bodyParameters = [.. context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => parameter.Name)];

        List<ValidationErrorDto> errors = [];
        HashSet<ValidationFailureKey> seen = [];
        foreach (KeyValuePair<string, ModelStateEntry> entry in context.ModelState)
        {
            if (entry.Value is not { Errors.Count: > 0 } state)
            {
                continue;
            }

            string field = ToField(entry.Key, bodyParameters);
            foreach (ModelError error in state.Errors)
            {
                ValidationFailure failure = ValidationFailure.TryParse(error.ErrorMessage, out ValidationFailure parsed)
                    ? parsed
                    : new ValidationFailure(ValidationErrorCodeDto.InvalidFormat);
                if (seen.Add(new ValidationFailureKey(field, failure)))
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = field,
                        Code = failure.Code,
                        Min = failure.Min,
                        Max = failure.Max,
                    });
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// The field of a key of the model state. Keys of malformed JSON are JSON paths (<c>$.year</c>, <c>$</c>); the name
    /// of the body parameter (e.g. <c>request</c> for a missing body) stands for the body as a whole and is removed as
    /// prefix. All become the field names of the API (<c>year</c>, empty for the body).
    /// </summary>
    public static string ToField(string key, IReadOnlySet<string> bodyParameters)
    {
        if (key == JsonPathRoot || bodyParameters.Contains(key))
        {
            return string.Empty;
        }

        if (key.StartsWith(JsonPathRoot + ".", StringComparison.Ordinal))
        {
            return key[(JsonPathRoot.Length + 1)..];
        }

        string? parameter = bodyParameters.FirstOrDefault(name => key.StartsWith(name + ".", StringComparison.Ordinal));
        if (parameter is not null)
        {
            return key[(parameter.Length + 1)..];
        }

        return key.StartsWith(JsonPathRoot + "[", StringComparison.Ordinal) ? key[JsonPathRoot.Length..] : key;
    }

    private readonly record struct ValidationFailureKey(string Field, ValidationFailure Failure);
}
