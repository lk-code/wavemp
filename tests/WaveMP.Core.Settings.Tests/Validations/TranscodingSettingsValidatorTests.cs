using AwesomeAssertions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Tests.Validations;

[TestFixture]
public class TranscodingSettingsValidatorTests
{
    [Test]
    public void Validate_Defaults_HasNoErrors()
    {
        TranscodingSettings defaults = new();

        TranscodingSettingsValidator.Validate(defaults).Should().BeEmpty();
        defaults.HardwareAcceleration.Should().Be(HardwareAccelerationMode.Auto);
        defaults.MaxParallelJobs.Should().Be(2);
        defaults.MaxVideoHeight.Should().Be(1080);
        defaults.AudioBitrateKbps.Should().Be(256);
    }

    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(8, false)]
    [TestCase(9, true)]
    public void Validate_MaxParallelJobs_MustBeWithinRange(int jobs, bool invalid)
    {
        IReadOnlyList<TranscodingSettingsValidationError> errors =
            TranscodingSettingsValidator.Validate(new TranscodingSettings { MaxParallelJobs = jobs });

        if (invalid)
        {
            errors.Should().Equal(new TranscodingSettingsValidationError(
                TranscodingSettingsField.MaxParallelJobs,
                SettingsErrorReason.OutOfRange,
                1,
                8));
        }
        else
        {
            errors.Should().BeEmpty();
        }
    }

    [TestCase(480, false)]
    [TestCase(720, false)]
    [TestCase(1440, false)]
    [TestCase(2160, false)]
    [TestCase(1000, true)]
    [TestCase(0, true)]
    public void Validate_MaxVideoHeight_MustBeAnAllowedHeight(int height, bool invalid)
    {
        IReadOnlyList<TranscodingSettingsValidationError> errors =
            TranscodingSettingsValidator.Validate(new TranscodingSettings { MaxVideoHeight = height });

        errors.Should().HaveCount(invalid ? 1 : 0);
        if (invalid)
        {
            errors[0].Should().Be(new TranscodingSettingsValidationError(
                TranscodingSettingsField.MaxVideoHeight,
                SettingsErrorReason.Invalid));
        }
    }

    [TestCase(128, false)]
    [TestCase(320, false)]
    [TestCase(96, true)]
    [TestCase(250, true)]
    public void Validate_AudioBitrate_MustBeAnAllowedBitrate(int bitrate, bool invalid)
    {
        IReadOnlyList<TranscodingSettingsValidationError> errors =
            TranscodingSettingsValidator.Validate(new TranscodingSettings { AudioBitrateKbps = bitrate });

        errors.Select(error => error.Field).Should().Equal(
            invalid ? [TranscodingSettingsField.AudioBitrateKbps] : []);
    }

    [Test]
    public void Validate_UndefinedMode_IsInvalid()
    {
        IReadOnlyList<TranscodingSettingsValidationError> errors = TranscodingSettingsValidator.Validate(
            new TranscodingSettings { HardwareAcceleration = (HardwareAccelerationMode)7 });

        errors.Should().Equal(new TranscodingSettingsValidationError(
            TranscodingSettingsField.HardwareAcceleration,
            SettingsErrorReason.Invalid));
    }
}
