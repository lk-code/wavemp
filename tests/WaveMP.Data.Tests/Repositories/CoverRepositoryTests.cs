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
        MediaFile firstAudio = seed.File(
            "Album/01 First.mp3",
            MediaFileKind.Audio,
            first,
            size: 4321,
            hasEmbeddedPicture: true);
        Cover firstCover = seed.TrackCover(first, firstAudio, CoverSource.Embedded, mimeType: "image/png");
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
                firstAudio.Id,
                null,
                first.Id,
                album.Id,
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
            MediaFileId = albumPicture.Id,
            AlbumId = (Guid?)album.Id,
            TrackId = (Guid?)null,
            TrackAlbumId = (Guid?)null,
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

    [Test]
    public async Task ListIdsWithCoversAsync_Library_ReturnsAlbumsWithCoversAndLooseTracksWithCovers()
    {
        LibrarySeed seed = new();
        Album withPicture = seed.Album("With picture");
        Album withTrackCover = seed.Album("With track cover");
        Album withoutCover = seed.Album("Without cover");
        Track ofAlbum = seed.Track("Of album", album: withTrackCover);
        seed.Track("Bare", album: withoutCover);
        Track looseWithCover = seed.Track("Loose with cover");
        seed.Track("Loose without cover");
        seed.AlbumCover(withPicture, seed.File("With picture/cover.jpg", MediaFileKind.Image));
        seed.TrackCover(ofAlbum, seed.File("With track cover/Of album.jpg", MediaFileKind.Image, ofAlbum));
        seed.TrackCover(looseWithCover, seed.File("Loose with cover.jpg", MediaFileKind.Image, looseWithCover));
        await SaveAsync(seed);
        CoverRepository repository = new(CreateDbContext());

        IReadOnlyList<Guid> albums = await repository.ListAlbumIdsWithCoversAsync();
        IReadOnlyList<Guid> tracks = await repository.ListLooseTrackIdsWithCoversAsync();

        albums.Should().Equal(new[] { withPicture.Id, withTrackCover.Id }.Order());
        tracks.Should().Equal(looseWithCover.Id);
    }

    [Test]
    public async Task GetBatchCandidatesAsync_AlbumsAndTracks_ReturnTheirCoversWithTheAlbumOfTheTrack()
    {
        LibrarySeed seed = new();
        Album first = seed.Album("First");
        Album second = seed.Album("Second");
        Album other = seed.Album("Other");
        Track firstTrack = seed.Track("First track", album: first);
        Track loose = seed.Track("Loose");
        Track otherLoose = seed.Track("Other loose");
        Cover firstPicture = seed.AlbumCover(first, seed.File("First/cover.jpg", MediaFileKind.Image));
        Cover firstTrackCover = seed.TrackCover(
            firstTrack,
            seed.File("First/First track.jpg", MediaFileKind.Image, firstTrack));
        Cover secondPicture = seed.AlbumCover(second, seed.File("Second/cover.jpg", MediaFileKind.Image));
        seed.AlbumCover(other, seed.File("Other/cover.jpg", MediaFileKind.Image));
        Cover looseCover = seed.TrackCover(loose, seed.File("Loose.jpg", MediaFileKind.Image, loose));
        seed.TrackCover(otherLoose, seed.File("Other loose.jpg", MediaFileKind.Image, otherLoose));
        await SaveAsync(seed);
        CoverRepository repository = new(CreateDbContext());

        IReadOnlyList<CoverCandidateRow> albums = await repository.GetAlbumBatchCandidatesAsync([first.Id, second.Id]);
        IReadOnlyList<CoverCandidateRow> tracks = await repository.GetTrackBatchCandidatesAsync([loose.Id]);

        albums.Select(candidate => candidate.CoverId).Should().BeEquivalentTo(
            [firstPicture.Id, firstTrackCover.Id, secondPicture.Id]);
        albums.Single(candidate => candidate.CoverId == firstTrackCover.Id).TrackAlbumId.Should().Be(first.Id);
        tracks.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            CoverId = looseCover.Id,
            TrackId = (Guid?)loose.Id,
            TrackAlbumId = (Guid?)null,
        });
        (await repository.GetAlbumBatchCandidatesAsync([])).Should().BeEmpty();
    }

    [Test]
    public async Task ListCoverSourcesAsync_FileWithSeveralCovers_ReturnsEachVersionOnce()
    {
        LibrarySeed seed = new();
        Album album = seed.Album("Album");
        Track track = seed.Track("Track", album: album);
        MediaFile picture = seed.File("Album/cover.jpg", MediaFileKind.Image, size: 555);
        MediaFile audio = seed.File("Album/Track.mp3", MediaFileKind.Audio, track, hasEmbeddedPicture: true);
        seed.AlbumCover(album, picture);
        seed.AlbumCover(album, picture, isPrimary: false);
        seed.TrackCover(track, audio, CoverSource.Embedded);
        await SaveAsync(seed);
        CoverRepository repository = new(CreateDbContext());

        IReadOnlyList<CoverSourceRow> sources = await repository.ListCoverSourcesAsync();

        sources.Should().BeEquivalentTo(
        [
            new CoverSourceRow(picture.Id, CoverSource.File, 555, LibrarySeed.DefaultTime),
            new CoverSourceRow(audio.Id, CoverSource.Embedded, 1000, LibrarySeed.DefaultTime),
        ]);
    }
}
