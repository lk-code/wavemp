using AwesomeAssertions;
using WaveMP.Core.Users.Equalizers;

namespace WaveMP.Core.Users.Tests.Equalizers;

[TestFixture]
public class EqualizerGainsTests
{
    [TestCase(-12)]
    [TestCase(0)]
    [TestCase(4.5)]
    [TestCase(12)]
    public void IsValid_GainWithinRange_IsTrue(double gain)
    {
        EqualizerGains.IsValid(gain).Should().BeTrue();
    }

    [TestCase(-12.1)]
    [TestCase(12.5)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void IsValid_GainOutOfRange_IsFalse(double gain)
    {
        EqualizerGains.IsValid(gain).Should().BeFalse();
    }

    [Test]
    public void IsValid_OneGainPerBand_IsTrue()
    {
        EqualizerGains.IsValid([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]).Should().BeTrue();
    }

    [TestCase(9)]
    [TestCase(11)]
    [TestCase(0)]
    public void IsValid_WrongNumberOfGains_IsFalse(int count)
    {
        EqualizerGains.IsValid(new double[count]).Should().BeFalse();
    }

    [Test]
    public void IsValid_CurveWithAGainOutOfRange_IsFalse()
    {
        EqualizerGains.IsValid([0, 0, 0, 0, 0, 13, 0, 0, 0, 0]).Should().BeFalse();
    }

    [TestCase(1.24, 1.2)]
    [TestCase(1.25, 1.3)]
    [TestCase(-1.25, -1.3)]
    [TestCase(3, 3)]
    public void Normalize_Gain_RoundsToATenthOfADecibel(double gain, double expected)
    {
        EqualizerGains.Normalize(gain).Should().Be(expected);
    }

    [Test]
    public void Normalize_TinyNegativeGain_IsPositiveZero()
    {
        double result = EqualizerGains.Normalize(-0.04);

        double.IsNegative(result).Should().BeFalse();
        result.Should().Be(0);
    }

    [Test]
    public void Normalize_Curve_RoundsEveryGain()
    {
        EqualizerGains.Normalize([0.04, 1.26, -2.56]).Should().Equal(0, 1.3, -2.6);
    }
}
