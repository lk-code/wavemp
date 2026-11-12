using AwesomeAssertions;
using WaveMP.Core.Users.Equalizers;

namespace WaveMP.Core.Users.Tests.Equalizers;

[TestFixture]
public class EqualizerBandsTests
{
    [Test]
    public void All_Always_HasTenOctaveBandsFrom31HzTo16KHz()
    {
        EqualizerBands.All.Select(band => band.FrequencyHz)
            .Should().Equal(31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000);
        EqualizerBands.All.Should().HaveCount(EqualizerBands.Count);
    }

    [Test]
    public void All_Always_IsSortedByFrequency()
    {
        EqualizerBands.All.Select(band => band.FrequencyHz).Should().BeInAscendingOrder();
    }

    [Test]
    public void All_OuterBands_AreShelvesAndTheOthersPeakingFilters()
    {
        EqualizerBands.All[0].FilterType.Should().Be(EqualizerFilterType.LowShelf);
        EqualizerBands.All[^1].FilterType.Should().Be(EqualizerFilterType.HighShelf);
        EqualizerBands.All.Skip(1).SkipLast(1)
            .Should().OnlyContain(band =>
                band.FilterType == EqualizerFilterType.Peaking && band.Q == EqualizerBands.PeakingQ);
    }

    [Test]
    public void Limits_Always_AreSymmetric()
    {
        EqualizerBands.MinGainDb.Should().Be(-EqualizerBands.MaxGainDb);
    }
}
