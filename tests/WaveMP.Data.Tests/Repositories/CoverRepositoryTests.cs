using AwesomeAssertions;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class CoverRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_library_covers";

    [Test]
    public async Task GetTrackCandidatesAsync_TrackOfAnAlbum_ReturnsItsOwnTheAlbumAndTheOtherTracksCovers()
    {
        LibrarySeed seed = new();
        Album album = seed.Album("Album");
        Album other = seed.Album("Other");
        Track first = seed.Track("First", album: album, number: 1, disc: 1);
        Track second = seed.Track("Second", album: album, number: 2, disc: 1);
        Track elsewhere = seed.Track("Elsewhere", album: other, number: 1);
        MediaFile albumPicture = seed.File("Album/cover.jpg", MediaFileKind.Image);
        Cover albumCover = seed.AlbumCover(album, albumPicture);
        Cover firstCover = seed.TrackCover(
            first,
            seed.File("Album/01 First.mp3", MediaFileKind.Audio, first, size: 4321, hasEmbeddedPicture: true),
            CoverSource.Embedded,
            mimeType: "image/png");
        Cover secondCover = seed.TrackCover(second, seed.File("Album/02 Second.jpg", MediaFileKind.Image, second));
        seed.TrackCover(elsewhere, seed.File("Other/01 Elsewhere.jpg", MediaFileKind.Image, elsewhere));
        await SaveAsync(seed);
        CoverRepository repository = new(CreateDbContext());

        IReadOnlyList<CoverCandidateRow> candidates = (await repository.GetTrackCandidatesAsync(second.Id))!;

        candidates.Select(candidate => candidate.CoverId).Should().BeEquivalentTo(
            [albumCover.Id, firstCover.Id, secondCover.Id]);
        candidates.Single(candidate => candidate.CoverId == firstCover.Id).Should().BeEquivalentTo(
            new CoverCandidateRow(
                firstCover.Id,
                null,
                first.Id,
                1,
                1,
                "First",
                true,
                CoverSource.Embedded,
                "Album/01 First.mp3",
                "image/png",
                4321,
                LibrarySeed.DefaultTime));
        candidates.Single(candidate => candidate.CoverId == albumCover.Id).Should().BeEquivalentTo(new
        {
            AlbumId = (Guid?)album.Id,
            TrackId = (Guid?)null,
            TrackTitle = (string?)null,
            Source = CoverSource.File,
            RelativePath = "Album/cover.jpg",
        });
    }

    [Test]
    public async Task GetTrackCandidatesAsync_TrackWithoutAlbum_ReturnsOnlyItsOwnCovers()
    {
        LibrarySeed seed = new();
        Track loose = seed.Track("Loose");
        Track other = seed.Track("Other");
        Cover cover = seed.TrackCover(loose, seed.File("Loose.jpg", MediaFileKind.Image, loose));
        seed.TrackCover(other, seed.File("Other.jpg", MediaFileKind.Image, other));
        await SaveAsync(seed);
        CoverRepository repository = new(CreateDbContext());

        IReadOnlyList<CoverCandidateRow> candidates = (await repository.GetTrackCandidatesAsync(loose.Id))!;

        candidates.Select(candidate => candidate.CoverId).Should().Equal(cover.Id);
        (await repository.GetTrackCandidatesAsync(seed.Track("Unsaved").Id)).Should().BeNull();
    }

    [Test]
    public async Task GetAlbumCandidatesAsync_Album_ReturnsItsCoversAndTheCoversOfItsTracks()
    {
        LibrarySeed seed = new();
        Album album = seed.Album("Album");
        Album empty = seed.Album("Empty");
        Track track = seed.Track("Track", album: album);
        Cover albumCover = seed.AlbumCover(album, seed.File("Album/folder.png", MediaFileKind.Image), mimeType: null);
        Cover trackCover = seed.TrackCover(track, seed.File("Album/Track.jpg", MediaFileKind.Image, track));
        await SaveAsync(seed);
        CoverRepository repository = new(CreateDbContext());

        IReadOnlyList<CoverCandidateRow> candidates = (await repository.GetAlbumCandidatesAsync(album.Id))!;

        candidates.Select(candidate => candidate.CoverId).Should().BeEquivalentTo([albumCover.Id, trackCover.Id]);
        (await repository.GetAlbumCandidatesAsync(empty.Id)).Should().BeEmpty();
        (await repository.GetAlbumCandidatesAsync(Guid.NewGuid())).Should().BeNull();
    }
}
