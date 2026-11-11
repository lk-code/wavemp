using AwesomeAssertions;
using WaveMP.Core.Caching.Covers;

namespace WaveMP.Core.Caching.Tests.Covers;

[TestFixture]
public class CoverSizesTests
{
    [TestCase(1, 96)]
    [TestCase(96, 96)]
    [TestCase(97, 300)]
    [TestCase(150, 300)]
    [TestCase(300, 300)]
    [TestCase(301, 1000)]
    [TestCase(1000, 1000)]
    [TestCase(2000, 1000)]
    [TestCase(4096, 1000)]
    public void Snap_RequestedSize_RoundsUpToADefinedSize(int requested, int expected)
    {
        CoverSizes.Snap(requested).Should().Be(expected);
    }

    [Test]
    public void Sizes_Always_PrepareTheSmallerOnes()
    {
        CoverSizes.All.Should().Equal(96, 300, 1000);
        CoverSizes.Prepared.Should().Equal(96, 300);
    }
}
