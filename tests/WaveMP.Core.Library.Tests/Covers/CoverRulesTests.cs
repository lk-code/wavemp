using AwesomeAssertions;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class CoverRulesTests
{
    [TestCase(CoverPriority.AlbumFirst, null, null, CoverPriority.AlbumFirst)]
    [TestCase(CoverPriority.TrackFirst, null, null, CoverPriority.TrackFirst)]
    [TestCase(CoverPriority.AlbumFirst, CoverPriority.TrackFirst, null, CoverPriority.TrackFirst)]
    [TestCase(CoverPriority.TrackFirst, CoverPriority.TrackFirst, CoverPriority.AlbumFirst, CoverPriority.AlbumFirst)]
    [TestCase(CoverPriority.TrackFirst, null, CoverPriority.AlbumFirst, CoverPriority.AlbumFirst)]
    public void Effective_Always_TakesTheMostSpecificRule(
        CoverPriority global,
        CoverPriority? album,
        CoverPriority? track,
        CoverPriority expected)
    {
        CoverRules.Effective(global, album, track).Should().Be(expected);
    }
}
