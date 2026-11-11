using AwesomeAssertions;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class CoverChoicesTests
{
    private static readonly Guid _albumId = Guid.NewGuid();
    private static readonly Guid _firstTrackId = Guid.NewGuid();
    private static readonly Guid _secondTrackId = Guid.NewGuid();

    private static readonly CoverCandidateRow _album = Cover(albumId: _albumId);
    private static readonly CoverCandidateRow _first = Cover(trackId: _firstTrackId, number: 1);
    private static readonly CoverCandidateRow _second = Cover(trackId: _secondTrackId, number: 2);

    [Test]
    public void ForAlbum_AlbumFirstWithAlbumCover_ListsTheAlbumAndEveryTrackWithOwnCovers()
    {
        IReadOnlyList<IReadOnlyList<CoverCandidateRow>> lists =
            CoverChoices.ForAlbum(CoverPriority.AlbumFirst, [_second, _album, _first]);

        // The album, its tracks without own cover and the first track share one list; the second track puts its own
        // cover before the cover of the first track.
        lists.Select(CoverChoices.KeyOf).Should().BeEquivalentTo(
        [
            CoverChoices.KeyOf([_album, _first, _second]),
            CoverChoices.KeyOf([_album, _second, _first]),
        ]);
        lists.Should().AllSatisfy(list => list[0].Should().Be(_album));
    }

    [Test]
    public void ForAlbum_TrackFirst_ListsTheAlbumEveryTrackWithOwnCoversAndTheOthers()
    {
        IReadOnlyList<IReadOnlyList<CoverCandidateRow>> lists =
            CoverChoices.ForAlbum(CoverPriority.TrackFirst, [_second, _album, _first]);

        CoverCandidateRow[] ofFirst = [_first, _album, _second];
        CoverCandidateRow[] ofSecond = [_second, _album, _first];
        bool firstIsLower = _firstTrackId.CompareTo(_secondTrackId) < 0;

        lists.Select(CoverChoices.KeyOf).Should().Equal(
            CoverChoices.KeyOf([_first, _second, _album]),
            CoverChoices.KeyOf(firstIsLower ? ofFirst : ofSecond),
            CoverChoices.KeyOf(firstIsLower ? ofSecond : ofFirst),
            CoverChoices.KeyOf([_album, _first, _second]));
    }

    [Test]
    public void ForAlbum_WithoutCovers_ListsNothing()
    {
        CoverChoices.ForAlbum(CoverPriority.AlbumFirst, []).Should().BeEmpty();
    }

    [Test]
    public void ForTrack_LooseTrack_ListsItsCovers()
    {
        CoverCandidateRow embedded = _first with
        {
            CoverId = Guid.NewGuid(),
            Source = CoverSource.Embedded,
            IsPrimary = false,
        };

        CoverChoices.ForTrack(_firstTrackId, CoverPriority.AlbumFirst, [embedded, _first])
            .Should().Equal(_first, embedded);
    }

    [Test]
    public void KeyOf_SamePicturesOfOtherCovers_IsTheSame()
    {
        CoverCandidateRow sameFile = _album with { CoverId = Guid.NewGuid(), AlbumId = Guid.NewGuid() };
        CoverCandidateRow embedded = _album with { Source = CoverSource.Embedded };

        CoverChoices.KeyOf([_album, _first]).Should().Be(CoverChoices.KeyOf([sameFile, _first]));
        CoverChoices.KeyOf([_album]).Should().NotBe(CoverChoices.KeyOf([embedded]));
        CoverChoices.KeyOf([_album, _first]).Should().NotBe(CoverChoices.KeyOf([_first, _album]));
    }

    private static CoverCandidateRow Cover(Guid? albumId = null, Guid? trackId = null, int? number = null)
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            albumId,
            trackId,
            trackId is null ? null : _albumId,
            trackId is null ? null : 1,
            number,
            trackId is null ? null : $"Track {number}",
            true,
            CoverSource.File,
            "Album/picture.jpg",
            "image/jpeg",
            100,
            DateTimeOffset.UnixEpoch);
    }
}
