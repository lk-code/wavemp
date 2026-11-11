using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class CoverServiceTests
{
    private readonly Guid _trackId = Guid.NewGuid();
    private readonly Guid _albumId = Guid.NewGuid();

    private ICoverRepository _covers = null!;
    private ISettingsSnapshot<LibrarySettings> _settings = null!;
    private CoverService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _covers = Substitute.For<ICoverRepository>();
        _covers.ListAlbumIdsWithCoversAsync(Arg.Any<CancellationToken>()).Returns([]);
        _covers.ListLooseTrackIdsWithCoversAsync(Arg.Any<CancellationToken>()).Returns([]);
        _settings = Substitute.For<ISettingsSnapshot<LibrarySettings>>();
        _settings.Current.Returns(new LibrarySettings());
        _service = new CoverService(_covers, _settings);
    }

    [Test]
    public async Task GetTrackCandidatesAsync_UnknownTrack_ReturnsOwnerNotFound()
    {
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CoverCandidateRow>?)null);

        CoverCandidates candidates = await _service.GetTrackCandidatesAsync(_trackId);

        candidates.Should().Be(CoverCandidates.OwnerNotFound);
        candidates.OwnerExists.Should().BeFalse();
    }

    [Test]
    public async Task GetTrackCandidatesAsync_AlbumAndOwnCover_PutsTheAlbumCoverFirstByDefault()
    {
        CoverCandidateRow album = AlbumCover();
        CoverCandidateRow own = TrackCover(_trackId);
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([own, album]);

        CoverCandidates candidates = await _service.GetTrackCandidatesAsync(_trackId);

        candidates.OwnerExists.Should().BeTrue();
        candidates.Ordered.Should().Equal(album, own);
    }

    [Test]
    public async Task GetTrackCandidatesAsync_TrackFirst_PutsTheOwnCoverFirst()
    {
        _settings.Current.Returns(new LibrarySettings { CoverPriority = CoverPriority.TrackFirst });
        CoverCandidateRow album = AlbumCover();
        CoverCandidateRow own = TrackCover(_trackId);
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([album, own]);

        CoverCandidates candidates = await _service.GetTrackCandidatesAsync(_trackId);

        candidates.Ordered.Should().Equal(own, album);
    }

    [Test]
    public async Task GetAlbumCandidatesAsync_UnknownAndKnownAlbum_ReturnsOwnerNotFoundOrTheOrderedCovers()
    {
        CoverCandidateRow track = TrackCover(_trackId);
        CoverCandidateRow album = AlbumCover();
        _covers.GetAlbumCandidatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CoverCandidateRow>?)null);
        _covers.GetAlbumCandidatesAsync(_albumId, Arg.Any<CancellationToken>()).Returns([track, album]);

        CoverCandidates missing = await _service.GetAlbumCandidatesAsync(Guid.NewGuid());
        CoverCandidates known = await _service.GetAlbumCandidatesAsync(_albumId);

        missing.OwnerExists.Should().BeFalse();
        known.Ordered.Should().Equal(album, track);
    }

    [Test]
    public async Task EnumerateShownCoversAsync_AlbumsAndLooseTracks_ListsEveryDistinctListOnce()
    {
        var otherAlbumId = Guid.NewGuid();
        var looseId = Guid.NewGuid();
        CoverCandidateRow album = AlbumCover();
        CoverCandidateRow own = TrackCover(_trackId);
        CoverCandidateRow otherAlbum = AlbumCover() with { AlbumId = otherAlbumId };
        CoverCandidateRow loose = TrackCover(looseId) with { TrackAlbumId = null };
        _covers.ListAlbumIdsWithCoversAsync(Arg.Any<CancellationToken>()).Returns([_albumId, otherAlbumId]);
        _covers.GetAlbumBatchCandidatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([own, otherAlbum, album]);
        _covers.ListLooseTrackIdsWithCoversAsync(Arg.Any<CancellationToken>()).Returns([looseId]);
        _covers.GetTrackBatchCandidatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([loose]);

        List<IReadOnlyList<CoverCandidateRow>> lists = [];
        await foreach (IReadOnlyList<CoverCandidateRow> list in _service.EnumerateShownCoversAsync())
        {
            lists.Add(list);
        }

        lists.Select(CoverChoices.KeyOf).Should().OnlyHaveUniqueItems();
        lists.Should().ContainEquivalentOf(new[] { album, own }, options => options.WithStrictOrdering());
        lists.Should().ContainEquivalentOf(new[] { otherAlbum }, options => options.WithStrictOrdering());
        lists.Should().ContainEquivalentOf(new[] { loose }, options => options.WithStrictOrdering());
        lists.Should().HaveCount(3);
    }

    [Test]
    public async Task EnumerateShownCoversAsync_ManyAlbums_ReadsThemInBatches()
    {
        Guid[] albumIds = [.. Enumerable.Range(0, CoverService.AlbumBatchSize + 1).Select(_ => Guid.NewGuid())];
        _covers.ListAlbumIdsWithCoversAsync(Arg.Any<CancellationToken>()).Returns(albumIds);
        _covers.GetAlbumBatchCandidatesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await foreach (IReadOnlyList<CoverCandidateRow> _ in _service.EnumerateShownCoversAsync())
        {
        }

        await _covers.Received(1).GetAlbumBatchCandidatesAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == CoverService.AlbumBatchSize),
            Arg.Any<CancellationToken>());
        await _covers.Received(1).GetAlbumBatchCandidatesAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1),
            Arg.Any<CancellationToken>());
    }

    private CoverCandidateRow AlbumCover()
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _albumId,
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

    private CoverCandidateRow TrackCover(Guid trackId)
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            trackId,
            _albumId,
            1,
            1,
            "Song",
            true,
            CoverSource.File,
            "Album/Song.jpg",
            null,
            100,
            DateTimeOffset.UnixEpoch);
    }
}
