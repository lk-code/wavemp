using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;
using WaveMP.Service.Validators;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace WaveMP.Service.Tests.Validators;

[TestFixture]
public class ValidationAttributeFailuresTests
{
    private static IEnumerable<TestCaseData> Cases()
    {
        yield return Case(new RequiredAttribute(), null, new(ValidationErrorCodeDto.Required))
            .SetArgDisplayNames("Required");
        yield return Case(new StringLengthAttribute(5), "too long", new(ValidationErrorCodeDto.MaxLength, Max: 5))
            .SetArgDisplayNames("StringLengthTooLong");
        yield return Case(
                new StringLengthAttribute(50) { MinimumLength = 2 },
                "a",
                new(ValidationErrorCodeDto.MinLength, Min: 2))
            .SetArgDisplayNames("StringLengthTooShort");
        yield return Case(new MinLengthAttribute(2), "a", new(ValidationErrorCodeDto.MinLength, Min: 2))
            .SetArgDisplayNames("MinLengthText");
        yield return Case(new MinLengthAttribute(2), new[] { "a" }, new(ValidationErrorCodeDto.MinItems, Min: 2))
            .SetArgDisplayNames("MinLengthList");
        yield return Case(new MaxLengthAttribute(1), "ab", new(ValidationErrorCodeDto.MaxLength, Max: 1))
            .SetArgDisplayNames("MaxLengthText");
        yield return Case(
                new MaxLengthAttribute(1),
                new List<string> { "a", "b" },
                new(ValidationErrorCodeDto.MaxItems, Max: 1))
            .SetArgDisplayNames("MaxLengthList");
        yield return Case(new LengthAttribute(2, 3), new[] { "a" }, new(ValidationErrorCodeDto.MinItems, Min: 2))
            .SetArgDisplayNames("LengthListTooShort");
        yield return Case(new LengthAttribute(2, 3), "abcd", new(ValidationErrorCodeDto.MaxLength, Max: 3))
            .SetArgDisplayNames("LengthTextTooLong");
        yield return Case(new RangeAttribute(1, 100), 0, new(ValidationErrorCodeDto.OutOfRange, 1, 100))
            .SetArgDisplayNames("RangeOfIntegers");
        yield return Case(new RangeAttribute(0.5, 1.5), 2.0, new(ValidationErrorCodeDto.OutOfRange))
            .SetArgDisplayNames("RangeOfFractions");
        yield return Case(new RegularExpressionAttribute("^a$"), "b", new(ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("Pattern");
        yield return Case(new EmailAddressAttribute(), "nobody", new(ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("DataType");
        yield return Case(new AllowedValuesAttribute("a"), "b", new(ValidationErrorCodeDto.InvalidValue))
            .SetArgDisplayNames("Other");
    }

    [TestCaseSource(nameof(Cases))]
    public void For_FailedAttribute_ReturnsCodeAndLimits(
        ValidationAttribute attribute,
        object? value,
        ValidationFailure expected)
    {
        attribute.IsValid(value).Should().BeFalse("the case must describe a failed attribute");

        ValidationFailure failure = ValidationAttributeFailures.For(attribute, value);

        failure.Should().Be(expected);
    }

    [Test]
    public void RequiredValue_DefaultOfValueType_IsInvalid()
    {
        RequiredValueAttribute attribute = new();

        attribute.IsValid(Guid.Empty).Should().BeFalse();
        attribute.IsValid(null).Should().BeFalse();
        attribute.IsValid(Guid.NewGuid()).Should().BeTrue();
        attribute.IsValid("text").Should().BeTrue();
        ValidationAttributeFailures.For(attribute, Guid.Empty)
            .Should().Be(new ValidationFailure(ValidationErrorCodeDto.Required));
    }

    private static TestCaseData Case(ValidationAttribute attribute, object? value, ValidationFailure expected)
    {
        return new TestCaseData(attribute, value, expected);
    }
}
