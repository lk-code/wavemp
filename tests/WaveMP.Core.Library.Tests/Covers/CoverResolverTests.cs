using AwesomeAssertions;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class CoverResolverTests
{
    private static readonly Guid _albumId = Guid.NewGuid();
    private static readonly Guid _trackId = Guid.NewGuid();
    private static readonly Guid _firstTrackId = Guid.NewGuid();
    private static readonly Guid _secondTrackId = Guid.NewGuid();

    private static readonly CoverCandidateRow _album = AlbumCover("album");
    private static readonly CoverCandidateRow _own = TrackCover("own", _trackId, 1, 5);
    private static readonly CoverCandidateRow _secondTrack = TrackCover("second", _secondTrackId, 2, 1);
    private static readonly CoverCandidateRow _firstTrack = TrackCover("first", _firstTrackId, 1, 1);

    [Test]
    public void OrderForTrack_AlbumFirst_PutsTheAlbumCoverBeforeTheOwnAndTheOtherTracksCovers()
    {
        IReadOnlyList<CoverCandidateRow> ordered = CoverResolver.OrderForTrack(
            _trackId,
            CoverPriority.AlbumFirst,
            [_secondTrack, _own, _firstTrack, _album]);

        ordered.Should().Equal(_album, _own, _firstTrack, _secondTrack);
    }

    [Test]
    public void OrderForTrack_TrackFirst_PutsTheOwnCoverFirst()
    {
        IReadOnlyList<CoverCandidateRow> ordered = CoverResolver.OrderForTrack(
            _trackId,
            CoverPriority.TrackFirst,
            [_secondTrack, _album, _firstTrack, _own]);

        ordered.Should().Equal(_own, _album, _firstTrack, _secondTrack);
    }

    [Test]
    public void OrderForTrack_WithoutAlbumCover_FallsBackToTheOwnCover()
    {
        IReadOnlyList<CoverCandidateRow> ordered = CoverResolver.OrderForTrack(
            _trackId,
            CoverPriority.AlbumFirst,
            [_firstTrack, _own]);

        ordered.Should().Equal(_own, _firstTrack);
    }

    [Test]
    public void OrderForTrack_SeveralOwnCovers_PutsThePrimaryAndPictureFilesFirst()
    {
        CoverCandidateRow embedded = _own with
        {
            CoverId = Guid.NewGuid(),
            Source = CoverSource.Embedded,
            IsPrimary = false,
        };
        CoverCandidateRow secondary = _own with { CoverId = Guid.NewGuid(), IsPrimary = false };

        IReadOnlyList<CoverCandidateRow> ordered = CoverResolver.OrderForTrack(
            _trackId,
            CoverPriority.TrackFirst,
            [embedded, secondary, _own]);

        ordered.Should().Equal(_own, secondary, embedded);
    }

    [TestCase(CoverPriority.AlbumFirst)]
    [TestCase(CoverPriority.TrackFirst)]
    public void OrderForAlbum_Always_FollowsTheRule(CoverPriority priority)
    {
        IReadOnlyList<CoverCandidateRow> ordered = CoverResolver.OrderForAlbum(
            priority,
            [_secondTrack, _album, _firstTrack]);

        if (priority == CoverPriority.AlbumFirst)
        {
            ordered.Should().Equal(_album, _firstTrack, _secondTrack);
        }
        else
        {
            ordered.Should().Equal(_firstTrack, _secondTrack, _album);
        }
    }

    private static CoverCandidateRow AlbumCover(string name)
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            _albumId,
            null,
            null,
            null,
            null,
            true,
            CoverSource.File,
            $"Album/{name}.jpg",
            "image/jpeg",
            100,
            DateTimeOffset.UnixEpoch);
    }

    private static CoverCandidateRow TrackCover(string name, Guid trackId, int disc, int number)
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            null,
            trackId,
            disc,
            number,
            name,
            true,
            CoverSource.File,
            $"Album/{name}.jpg",
            "image/jpeg",
            100,
            DateTimeOffset.UnixEpoch);
    }
}
