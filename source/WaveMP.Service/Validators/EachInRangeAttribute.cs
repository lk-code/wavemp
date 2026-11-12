using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Validators;

/// <summary>
/// Checks that every number of a list is finite and from <see cref="Minimum"/> to <see cref="Maximum"/>. A missing list
/// is valid; <see cref="RequiredAttribute"/> checks for it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class EachInRangeAttribute(int minimum, int maximum) : ValidationAttribute
{
    public int Minimum { get; } = minimum;

    public int Maximum { get; } = maximum;

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not IEnumerable items || value is string)
        {
            return false;
        }

        foreach (object? item in items)
        {
            double number = item switch
            {
                double fraction => fraction,
                float fraction => fraction,
                decimal fraction => (double)fraction,
                int whole => whole,
                long whole => whole,
                _ => double.NaN,
            };

            if (!double.IsFinite(number) || number < Minimum || number > Maximum)
            {
                return false;
            }
        }

        return true;
    }
}
