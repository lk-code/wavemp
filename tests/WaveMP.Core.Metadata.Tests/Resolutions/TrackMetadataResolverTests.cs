using AwesomeAssertions;
using WaveMP.Core.Metadata.Paths;
using WaveMP.Core.Metadata.Resolutions;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Tests.Resolutions;

[TestFixture]
public class TrackMetadataResolverTests
{
    [Test]
    public void Resolve_WithTags_PrefersTheTagsOverMisleadingFolders()
    {
        MediaTags tags = new()
        {
            Title = "Real Title",
            Artist = "Real Artist",
            Album = "Real Album",
            TrackNumber = 4,
            Year = 1999,
            Genre = "Jazz",
        };
        PathMetadata path = PathMetadataParser.Parse(["Wrong Artist", "Wrong Album"], "01 - Wrong Title");

        ResolvedTrackMetadata result = TrackMetadataResolver.Resolve(tags, path);

        result.Should().Be(new ResolvedTrackMetadata(
            "Real Title",
            TrackMetadataSource.Tags,
            "Real Artist",
            null,
            "Real Album",
            4,
            null,
            1999,
            "Jazz"));
    }

    [Test]
    public void Resolve_WithIncompleteTags_TakesTheMissingFieldsFromThePath()
    {
        MediaTags tags = new() { Title = "Tagged Title", Artist = "Artist" };
        PathMetadata path = PathMetadataParser.Parse(["Artist", "Folder Album", "CD2"], "07 Name");

        ResolvedTrackMetadata result = TrackMetadataResolver.Resolve(tags, path);

        result.Title.Should().Be("Tagged Title");
        result.Album.Should().Be("Folder Album");
        result.TrackNumber.Should().Be(7);
        result.DiscNumber.Should().Be(2);
    }

    [Test]
    public void Resolve_WithoutTags_UsesThePathAsSource()
    {
        PathMetadata path = PathMetadataParser.Parse(["Artist", "Album"], "01 - Title");

        ResolvedTrackMetadata result = TrackMetadataResolver.Resolve(MediaTags.Empty, path);

        result.Title.Should().Be("Title");
        result.TitleSource.Should().Be(TrackMetadataSource.Path);
        result.Artist.Should().Be("Artist");
    }

    [Test]
    public void Resolve_WithNothing_UsesTheFileNameAsTitle()
    {
        PathMetadata path = PathMetadataParser.Parse([], "weird");

        ResolvedTrackMetadata result = TrackMetadataResolver.Resolve(MediaTags.Empty, path);

        result.Title.Should().Be("weird");
        result.TitleSource.Should().Be(TrackMetadataSource.FileName);
        result.Artist.Should().BeNull();
        result.Album.Should().BeNull();
    }

    [Test]
    public void Resolve_SamplerFolderWithoutTags_MakesTheFolderArtistTheAlbumArtist()
    {
        PathMetadata path = PathMetadataParser.Parse(["Various", "Hits"], "01 - A - X");

        ResolvedTrackMetadata result = TrackMetadataResolver.Resolve(MediaTags.Empty, path);

        result.Artist.Should().Be("A");
        result.AlbumArtist.Should().Be("Various");
    }

    [Test]
    public void Resolve_TagArtistOtherThanTheFolder_LeavesTheAlbumArtistOpen()
    {
        PathMetadata path = PathMetadataParser.Parse(["Various", "Hits"], "01 - A - X");

        ResolvedTrackMetadata result = TrackMetadataResolver.Resolve(new MediaTags { Artist = "B" }, path);

        result.Artist.Should().Be("B");
        result.AlbumArtist.Should().BeNull();
    }
}
