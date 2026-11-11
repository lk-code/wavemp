using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Caching.Tests.Covers;

[TestFixture]
public class CoverCacheServiceTests
{
    private readonly Guid _id = Guid.NewGuid();
    private ICoverService _covers = null!;
    private ICoverImageCache _cache = null!;
    private CoverCacheService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _covers = Substitute.For<ICoverService>();
        _cache = Substitute.For<ICoverImageCache>();
        _service = new CoverCacheService(_covers, _cache);
    }

    [Test]
    public async Task GetTrackCoverAsync_UnknownTrack_ReturnsOwnerNotFound()
    {
        _covers.GetTrackCandidatesAsync(_id, Arg.Any<CancellationToken>()).Returns(CoverCandidates.OwnerNotFound);

        CoverDeliveryResult result = await _service.GetTrackCoverAsync(_id, 300);

        result.Should().Be(CoverDeliveryResult.OwnerNotFound);
        await _cache.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default, default);
    }

    [Test]
    public async Task GetTrackCoverAsync_WithCovers_PassesTheOrderedCoversAndTheSize()
    {
        CoverCandidateRow candidate = Candidate();
        CoverDelivery delivery = new(new MemoryStream([1]), "image/webp", "etag", DateTimeOffset.UnixEpoch);
        _covers.GetTrackCandidatesAsync(_id, Arg.Any<CancellationToken>())
            .Returns(new CoverCandidates(true, [candidate]));
        _cache.DeliverAsync(
                Arg.Is<IReadOnlyList<CoverCandidateRow>>(list => list.Single() == candidate),
                96,
                Arg.Any<CancellationToken>())
            .Returns(delivery);

        CoverDeliveryResult result = await _service.GetTrackCoverAsync(_id, 96);

        result.Should().Be(CoverDeliveryResult.Found(delivery));
    }

    [Test]
    public async Task GetAlbumCoverAsync_NothingDeliverable_ReturnsNotFound()
    {
        _covers.GetAlbumCandidatesAsync(_id, Arg.Any<CancellationToken>())
            .Returns(new CoverCandidates(true, [Candidate()]));
        _cache.DeliverAsync(Arg.Any<IReadOnlyList<CoverCandidateRow>>(), null, Arg.Any<CancellationToken>())
            .Returns((CoverDelivery?)null);

        CoverDeliveryResult result = await _service.GetAlbumCoverAsync(_id, null);

        result.Should().Be(CoverDeliveryResult.NotFound);
    }

    [Test]
    public async Task GetAlbumCoverAsync_UnknownAlbum_ReturnsOwnerNotFound()
    {
        _covers.GetAlbumCandidatesAsync(_id, Arg.Any<CancellationToken>()).Returns(CoverCandidates.OwnerNotFound);

        (await _service.GetAlbumCoverAsync(_id, 300)).Status.Should().Be(CoverStatus.OwnerNotFound);
    }

    private CoverCandidateRow Candidate()
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _id,
            null,
            null,
            null,
            null,
            null,
            true,
            CoverSource.File,
            "Album/cover.jpg",
            "image/jpeg",
            100,
            DateTimeOffset.UnixEpoch);
    }
}
