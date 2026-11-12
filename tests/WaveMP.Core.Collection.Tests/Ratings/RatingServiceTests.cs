using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Collection.Ratings;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.Tests.Ratings;

[TestFixture]
public class RatingServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _trackId = Guid.NewGuid();
    private ITrackRatingRepository _ratings = null!;
    private RatingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _ratings = Substitute.For<ITrackRatingRepository>();
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        _service = new RatingService(_ratings, clock);
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    public async Task SetAsync_Stars_SetsTheRating(int rating)
    {
        _ratings.SetAsync(_userId, _trackId, rating, _now, Arg.Any<CancellationToken>()).Returns(true);

        RatingStatus status = await _service.SetAsync(_userId, _trackId, rating);

        status.Should().Be(RatingStatus.Done);
        await _ratings.DidNotReceiveWithAnyArgs().RemoveAsync(default, default);
    }

    [Test]
    public async Task SetAsync_Zero_RemovesTheRating()
    {
        _ratings.RemoveAsync(_userId, _trackId, Arg.Any<CancellationToken>()).Returns(true);

        RatingStatus status = await _service.SetAsync(_userId, _trackId, 0);

        status.Should().Be(RatingStatus.Done);
        await _ratings.DidNotReceiveWithAnyArgs().SetAsync(default, default, default, default);
    }

    [TestCase(-1)]
    [TestCase(6)]
    [TestCase(int.MaxValue)]
    public async Task SetAsync_OutOfRange_IsInvalidAndChangesNothing(int rating)
    {
        RatingStatus status = await _service.SetAsync(_userId, _trackId, rating);

        status.Should().Be(RatingStatus.Invalid);
        _ratings.ReceivedCalls().Should().BeEmpty();
    }

    [TestCase(0)]
    [TestCase(4)]
    public async Task SetAsync_UnknownTrack_ReturnsNotFound(int rating)
    {
        RatingStatus status = await _service.SetAsync(_userId, _trackId, rating);

        status.Should().Be(RatingStatus.NotFound);
    }

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(5, true)]
    [TestCase(6, false)]
    public void IsValid_Always_AcceptsZeroToFive(int rating, bool expected)
    {
        TrackRatings.IsValid(rating).Should().Be(expected);
    }
}
