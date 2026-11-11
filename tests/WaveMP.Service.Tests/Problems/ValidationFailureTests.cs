using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Tests.Problems;

[TestFixture]
public class ValidationFailureTests
{
    private static IEnumerable<ValidationFailure> Failures()
    {
        yield return new ValidationFailure(ValidationErrorCodeDto.Required);
        yield return new ValidationFailure(ValidationErrorCodeDto.MaxLength, Max: 50);
        yield return new ValidationFailure(ValidationErrorCodeDto.MinItems, Min: 1);
        yield return new ValidationFailure(ValidationErrorCodeDto.OutOfRange, -5, int.MaxValue);
    }

    [TestCaseSource(nameof(Failures))]
    public void TryParse_MessageOfFailure_ReturnsTheSameFailure(ValidationFailure failure)
    {
        bool parsed = ValidationFailure.TryParse(failure.ToMessage(), out ValidationFailure result);

        parsed.Should().BeTrue();
        result.Should().Be(failure);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("The Name field is required.")]
    [TestCase("validation:")]
    [TestCase("validation:Unknown")]
    [TestCase("validation:7")]
    [TestCase("validation:Required;size=3")]
    [TestCase("validation:OutOfRange;min=abc")]
    public void TryParse_OtherText_ReturnsFalse(string? message)
    {
        bool parsed = ValidationFailure.TryParse(message, out _);

        parsed.Should().BeFalse();
    }
}
