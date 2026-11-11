using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Validators;

/// <summary>
/// Checks a value against a data annotation like the validator of the framework, but reports a failure as internal
/// message of a <see cref="ValidationFailure"/> (code and limits) instead of the English text of the attribute.
/// </summary>
public sealed class CodedAttributeValidator(ValidationAttribute attribute) : IModelValidator
{
    // ValidationContext needs an instance; the framework uses a placeholder as well when there is neither container
    // nor model.
    private static readonly object _emptyInstance = new();

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        ValidationContext validationContext = new(
            context.Container ?? context.Model ?? _emptyInstance,
            context.ActionContext.HttpContext.RequestServices,
            items: null)
        {
            DisplayName = context.ModelMetadata.GetDisplayName(),
            MemberName = context.ModelMetadata.Name,
        };

        if (attribute.GetValidationResult(context.Model, validationContext) == ValidationResult.Success)
        {
            return [];
        }

        // Without a member name the error belongs to the validated value itself, like a property-level attribute.
        string message = ValidationAttributeFailures.For(attribute, context.Model).ToMessage();
        return [new ModelValidationResult(memberName: null, message)];
    }
}
