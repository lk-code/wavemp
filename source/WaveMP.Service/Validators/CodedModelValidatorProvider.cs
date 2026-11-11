using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WaveMP.Service.Validators;

/// <summary>
/// Creates a <see cref="CodedAttributeValidator"/> for every data annotation (<see cref="ValidationAttribute"/>). It
/// runs before the provider of the framework, which skips attributes that already have a validator, so no English
/// default message ever reaches the model state.
/// </summary>
public sealed class CodedModelValidatorProvider : IModelValidatorProvider
{
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        foreach (ValidatorItem item in context.Results)
        {
            if (item.Validator is null && item.ValidatorMetadata is ValidationAttribute attribute)
            {
                item.Validator = new CodedAttributeValidator(attribute);
                item.IsReusable = true;
            }
        }
    }
}
