using AwesomeAssertions;
using WaveMP.Core.Scanning.Groupings;
using WaveMP.Core.Scanning.Processors;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Tests.Processors;

[TestFixture]
public class FolderProcessorTests
{
    [Test]
    public void Process_FolderWithTracksAndPictures_BuildsTracksWithTheirPrimaryFiles()
    {
        MediaFile mp3 = Audio("Band/Album/01 - One.mp3", lossless: false);
        MediaFile flac = Audio("Band/Album/01 - One.flac", lossless: true);
        MediaFile video = TestFiles.Create("Band/Album/01 - One.mkv", configure: file => file.Duration = null);
        MediaFile cover = TestFiles.Create("Band/Album/cover.jpg");

        ProcessedFolder folder = FolderProcessor.Process("Band/Album", [mp3, flac, video, cover]);

        ProcessedTrack track = folder.Tracks.Should().ContainSingle().Subject;
        track.PrimaryAudio.Should().BeSameAs(flac);
        track.PrimaryVideo.Should().BeSameAs(video);
        track.Duration.Should().Be(TimeSpan.FromSeconds(3));
        track.Metadata.Title.Should().Be("One");
        track.Metadata.TitleSource.Should().Be(TrackMetadataSource.Path);
        track.Metadata.TrackNumber.Should().Be(1);
        track.Album!.AlbumArtist.Should().Be("Band");
        folder.PictureGroups.Should().ContainSingle().Which.NormalizedBaseName.Should().Be("COVER");
    }

    [Test]
    public void Process_TagsOfThePrimaryFile_DecideTheMetadata()
    {
        MediaFile tagged = Audio("Wrong/Folder/x.flac", lossless: true, file => file.Tags = new MediaFileTags
        {
            Title = "Real",
            Artist = "Artist",
            Album = "Album",
        });
        MediaFile other = Audio("Wrong/Folder/x.mp3", lossless: false, file => file.Tags = new MediaFileTags
        {
            Title = "Other",
        });

        ProcessedTrack track = FolderProcessor.Process("Wrong/Folder", [other, tagged]).Tracks.Single();

        track.Metadata.Title.Should().Be("Real");
        track.Metadata.TitleSource.Should().Be(TrackMetadataSource.Tags);
        track.Album!.Title.Should().Be("Album");
    }

    [Test]
    public void Process_SeveralArtistsInOneFolder_MakesACompilation()
    {
        MediaFile first = Audio("Hits/a.mp3", false, file => file.Tags = new MediaFileTags
        {
            Artist = "A",
            Album = "Hits",
        });
        MediaFile second = Audio("Hits/b.mp3", false, file => file.Tags = new MediaFileTags
        {
            Artist = "B",
            Album = "Hits",
        });

        ProcessedFolder folder = FolderProcessor.Process("Hits", [first, second]);

        folder.Tracks.Should().AllSatisfy(track =>
        {
            track.Album!.IsCompilation.Should().BeTrue();
            track.Album.AlbumArtist.Should().BeNull();
        });
    }

    [Test]
    public void Process_TrackCovers_PictureFilesFirstThenEmbeddedPictures()
    {
        MediaFile primary = Audio("A/Song.flac", true, file => Embedded(file, 500));
        MediaFile secondary = Audio("A/Song.mp3", false, file => Embedded(file, 600));
        MediaFile video = TestFiles.Create("A/Song.mkv", configure: file => Embedded(file, 700));
        MediaFile small = TestFiles.Create("A/Song.jpg", configure: file => (file.Width, file.Height) = (100, 100));
        MediaFile large = TestFiles.Create("A/song.png", configure: file => (file.Width, file.Height) = (300, 300));

        ProcessedTrack track = FolderProcessor.Process("A", [primary, secondary, video, small, large]).Tracks.Single();

        track.Covers.Select(cover => (cover.File, cover.Source)).Should().Equal(
            (large, CoverSource.File),
            (small, CoverSource.File),
            (primary, CoverSource.Embedded),
            (secondary, CoverSource.Embedded),
            (video, CoverSource.Embedded));
        track.Covers[0].MimeType.Should().Be("image/png");
        track.Covers[2].Width.Should().Be(500);
    }

    [Test]
    public void Process_FileNamesWithoutTags_UseThePatternOfTheFolder()
    {
        MediaFile[] files =
        [
            Audio("X - A.mp3", false),
            Audio("Y - A.mp3", false),
            Audio("Z - A.mp3", false),
        ];

        ProcessedFolder folder = FolderProcessor.Process(string.Empty, files);

        folder.Tracks.Select(track => (track.Metadata.Title, track.Metadata.Artist))
            .Should().Equal(("X", "A"), ("Y", "A"), ("Z", "A"));
    }

    [Test]
    public void AlbumCoverCandidates_CoverNamesAndDiscFolders_AreFoundInTheOrderOfTheNames()
    {
        var album = Guid.NewGuid();
        var single = Guid.NewGuid();
        AlbumFolder[] folders =
        [
            new(album, "Band/Double/CD1"),
            new(album, "Band/Double/CD2"),
            new(single, "Band/Single"),
        ];
        AlbumCoverImage folderJpg = Image("Band/Single/folder.jpg", 500);
        AlbumCoverImage coverJpg = Image("Band/Single/cover.jpg", 100);
        AlbumCoverImage parent = Image("Band/Double/cover.png", 300);
        AlbumCoverImage other = Image("Band/Other/cover.jpg", 300);

        IReadOnlyDictionary<Guid, IReadOnlyList<AlbumCoverImage>> covers = AlbumCoverCandidates.Select(
            folders,
            [folderJpg, coverJpg, parent, other],
            ["cover", "folder"]);

        covers[single].Should().Equal(coverJpg, folderJpg);
        covers[album].Should().Equal(parent);
    }

    [TestCase("Album/CD1", "Album")]
    [TestCase("CD2", "")]
    [TestCase("Album", null)]
    public void ParentOfDiscFolder_Folder_IsTheFolderAboveDiscFolders(string directory, string? expected)
    {
        AlbumCoverCandidates.ParentOfDiscFolder(directory).Should().Be(expected);
    }

    private static MediaFile Audio(string path, bool lossless, Action<MediaFile>? configure = null)
    {
        return TestFiles.Create(path, MediaFileKind.Audio, file =>
        {
            file.IsLossless = lossless;
            file.Duration = TimeSpan.FromSeconds(3);
            configure?.Invoke(file);
        });
    }

    private static void Embedded(MediaFile file, int size)
    {
        file.HasEmbeddedPicture = true;
        file.EmbeddedPictureWidth = size;
        file.EmbeddedPictureHeight = size;
        file.EmbeddedPictureMimeType = "image/jpeg";
    }

    private static AlbumCoverImage Image(string path, int size)
    {
        int slash = path.LastIndexOf('/');
        string name = path[(slash + 1)..];
        return new AlbumCoverImage(
            Guid.NewGuid(),
            path,
            path[..slash],
            FileGrouper.NormalizeBaseName(name[..name.LastIndexOf('.')]),
            name[(name.LastIndexOf('.') + 1)..],
            1000,
            size,
            size);
    }
}
