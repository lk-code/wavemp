using AwesomeAssertions;
using WaveMP.Core.Metadata.Resolutions;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Tests.Resolutions;

[TestFixture]
public class AlbumArtistResolverTests
{
    [Test]
    public void Resolve_SameArtistEverywhere_MakesItTheAlbumArtist()
    {
        IReadOnlyList<AlbumAssignment?> result = AlbumArtistResolver.Resolve(
        [
            Track("Band", album: "Album", year: 2001),
            Track("band", album: "ALBUM"),
        ]);

        result.Should().AllSatisfy(album => album.Should().Be(new AlbumAssignment(
            "Album",
            "Band",
            false,
            2001,
            AlbumKeys.Create("Album", "Band", false))));
    }

    [Test]
    public void Resolve_SeveralArtistsWithoutAlbumArtist_IsACompilation()
    {
        IReadOnlyList<AlbumAssignment?> result = AlbumArtistResolver.Resolve(
        [
            Track("A", album: "Hits"),
            Track("B", album: "Hits"),
        ]);

        result[0]!.IsCompilation.Should().BeTrue();
        result[0]!.AlbumArtist.Should().BeNull();
        result[0]!.Key.Should().Be(AlbumKeys.Create("hits", null, true));
        result[1].Should().Be(result[0]);
    }

    [Test]
    public void Resolve_WithAlbumArtists_TakesTheMostFrequentOne()
    {
        IReadOnlyList<AlbumAssignment?> result = AlbumArtistResolver.Resolve(
        [
            Track("A", "Main", "Album"),
            Track("B", "Main", "Album"),
            Track("C", "Other", "Album"),
            Track("D", null, "Album"),
        ]);

        result.Should().AllSatisfy(album => album!.AlbumArtist.Should().Be("Main"));
        result[0]!.IsCompilation.Should().BeFalse();
    }

    [Test]
    public void Resolve_TracksWithoutAlbumOrArtist_GetNoAlbumOrAnAlbumWithoutArtist()
    {
        IReadOnlyList<AlbumAssignment?> result = AlbumArtistResolver.Resolve(
        [
            Track(null, album: "Unknown"),
            Track("A"),
        ]);

        result[0].Should().Be(new AlbumAssignment(
            "Unknown",
            null,
            false,
            null,
            AlbumKeys.Create("Unknown", null, false)));
        result[1].Should().BeNull();
    }

    [Test]
    public void Resolve_TwoAlbumsInOneFolder_ResolvesThemSeparately()
    {
        IReadOnlyList<AlbumAssignment?> result = AlbumArtistResolver.Resolve(
        [
            Track("A", album: "First"),
            Track("B", album: "Second"),
        ]);

        result[0]!.AlbumArtist.Should().Be("A");
        result[1]!.AlbumArtist.Should().Be("B");
    }

    [Test]
    public void AlbumKeys_SameNamesInOtherSpelling_HaveTheSameKey()
    {
        string key = AlbumKeys.Create(" Abbey  Road", "The Beatles", false);

        key.Should().Be(AlbumKeys.Create("ABBEY ROAD", "the beatles", false));
        key.Should().HaveLength(64);
        key.Should().NotBe(AlbumKeys.Create("Abbey Road", null, true));
        key.Should().NotBe(AlbumKeys.Create("Abbey Road", null, false));
    }

    private static ResolvedTrackMetadata Track(
        string? artist,
        string? albumArtist = null,
        string? album = null,
        int? year = null)
    {
        return new ResolvedTrackMetadata(
            "Title",
            TrackMetadataSource.Tags,
            artist,
            albumArtist,
            album,
            null,
            null,
            year,
            null);
    }
}
