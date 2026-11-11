using AwesomeAssertions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Tests.Validations;

[TestFixture]
public class CacheSettingsValidatorTests
{
    [Test]
    public void Validate_Defaults_HasNoErrors()
    {
        CacheSettings defaults = new();

        CacheSettingsValidator.Validate(defaults).Should().BeEmpty();
        defaults.MaxSizeMegabytes.Should().Be(2000);
        defaults.GetMaxSizeBytes().Should().Be(2_000_000_000);
    }

    [TestCase(99, true)]
    [TestCase(100, false)]
    [TestCase(1_000_000, false)]
    [TestCase(1_000_001, true)]
    [TestCase(-1, true)]
    public void Validate_MaxSize_MustBeWithinRange(int megabytes, bool invalid)
    {
        CacheSettings settings = new() { MaxSizeMegabytes = megabytes };

        IReadOnlyList<CacheSettingsValidationError> errors = CacheSettingsValidator.Validate(settings);

        if (invalid)
        {
            errors.Should().Equal(
                new CacheSettingsValidationError(
                    CacheSettingsField.MaxSizeMegabytes,
                    SettingsErrorReason.OutOfRange,
                    100,
                    1_000_000));
        }
        else
        {
            errors.Should().BeEmpty();
        }
    }

    [Test]
    public void GetMaxSizeBytes_LargestValue_DoesNotOverflow()
    {
        CacheSettings settings = new() { MaxSizeMegabytes = CacheSettingsValidator.MaxMaxSizeMegabytes };

        settings.GetMaxSizeBytes().Should().Be(1_000_000_000_000);
    }
}
