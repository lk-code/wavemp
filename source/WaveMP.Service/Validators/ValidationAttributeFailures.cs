using System.Collections;
using System.ComponentModel.DataAnnotations;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Validators;

/// <summary>
/// Describes why a value failed a data annotation as <see cref="ValidationFailure"/>, with the limits of the attribute
/// as parameters.
/// </summary>
public static class ValidationAttributeFailures
{
    /// <summary>
    /// The failure of <paramref name="value"/> for <paramref name="attribute"/>. Attributes without a more specific code
    /// become <see cref="ValidationErrorCodeDto.InvalidValue"/>.
    /// </summary>
    public static ValidationFailure For(ValidationAttribute attribute, object? value)
    {
        return attribute switch
        {
            RequiredAttribute => new(ValidationErrorCodeDto.Required),
            StringLengthAttribute stringLength => LengthFailure(
                value,
                stringLength.MinimumLength,
                stringLength.MaximumLength),
            LengthAttribute length => LengthFailure(value, length.MinimumLength, length.MaximumLength),
            MinLengthAttribute minLength => IsText(value)
                ? new(ValidationErrorCodeDto.MinLength, Min: minLength.Length)
                : new(ValidationErrorCodeDto.MinItems, Min: minLength.Length),
            MaxLengthAttribute maxLength => IsText(value)
                ? new(ValidationErrorCodeDto.MaxLength, Max: maxLength.Length)
                : new(ValidationErrorCodeDto.MaxItems, Max: maxLength.Length),
            EachInRangeAttribute eachInRange => new(
                ValidationErrorCodeDto.OutOfRange,
                eachInRange.Minimum,
                eachInRange.Maximum),
            RangeAttribute range => new(
                ValidationErrorCodeDto.OutOfRange,
                ToLimit(range.Minimum),
                ToLimit(range.Maximum)),
            RegularExpressionAttribute or DataTypeAttribute or Base64StringAttribute =>
                new(ValidationErrorCodeDto.InvalidFormat),
            _ => new(ValidationErrorCodeDto.InvalidValue),
        };
    }

    /// <summary>
    /// A text or list outside the length from <paramref name="minimum"/> to <paramref name="maximum"/>: too short if
    /// it has fewer entries than the minimum, otherwise too long.
    /// </summary>
    private static ValidationFailure LengthFailure(object? value, int minimum, int maximum)
    {
        bool isText = IsText(value);
        int? count = value switch
        {
            string text => text.Length,
            ICollection collection => collection.Count,
            IEnumerable enumerable => enumerable.Cast<object?>().Count(),
            _ => null,
        };

        if (count is { } actual && actual < minimum)
        {
            return isText
                ? new(ValidationErrorCodeDto.MinLength, Min: minimum)
                : new(ValidationErrorCodeDto.MinItems, Min: minimum);
        }

        return isText
            ? new(ValidationErrorCodeDto.MaxLength, Max: maximum)
            : new(ValidationErrorCodeDto.MaxItems, Max: maximum);
    }

    private static bool IsText(object? value)
    {
        // A missing value is checked by the length attributes of texts and lists alike; texts are far more common.
        return value is null or string;
    }

    private static int? ToLimit(object? limit)
    {
        return limit switch
        {
            int number => number,
            long number when number is >= int.MinValue and <= int.MaxValue => (int)number,

            // Limits of fractions only become parameters if they are whole numbers, e.g. -12.0 to 12.0 dB.
            double number when double.IsInteger(number) && number is >= int.MinValue and <= int.MaxValue =>
                (int)number,
            _ => null,
        };
    }
}
