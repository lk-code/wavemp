using AwesomeAssertions;
using WaveMP.Service.Validators;

namespace WaveMP.Service.Tests.Validators;

[TestFixture]
public class EachInRangeAttributeTests
{
    private readonly EachInRangeAttribute _attribute = new(-12, 12);

    [Test]
    public void IsValid_Null_IsTrue()
    {
        _attribute.IsValid(null).Should().BeTrue();
    }

    [Test]
    public void IsValid_NumbersWithinRange_IsTrue()
    {
        _attribute.IsValid(new[] { -12, 0, 4.5, 12 }).Should().BeTrue();
    }

    [Test]
    public void IsValid_EmptyList_IsTrue()
    {
        _attribute.IsValid(Array.Empty<double>()).Should().BeTrue();
    }

    [TestCase(-12.1)]
    [TestCase(12.5)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    public void IsValid_NumberOutOfRange_IsFalse(double number)
    {
        _attribute.IsValid(new[] { 0, number }).Should().BeFalse();
    }

    [Test]
    public void IsValid_ListOfIntegersOutOfRange_IsFalse()
    {
        _attribute.IsValid(new List<int> { 13 }).Should().BeFalse();
    }

    [Test]
    public void IsValid_NoList_IsFalse()
    {
        _attribute.IsValid("12").Should().BeFalse();
        _attribute.IsValid(new[] { "1" }).Should().BeFalse();
    }
}
