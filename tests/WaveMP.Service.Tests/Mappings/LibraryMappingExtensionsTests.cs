using AwesomeAssertions;
using WaveMP.Core.Library.Searches;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class LibraryMappingExtensionsTests
{
    private static readonly DateTimeOffset _time = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void ToDto_Track_MapsArtistAlbumAndRoundsTheDuration()
    {
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var albumArtistId = Guid.NewGuid();
        TrackRow row = new(
            Guid.NewGuid(),
            "Closing",
            artistId,
            "Alpha Band",
            albumId,
            "First Album",
            albumArtistId,
            "Alpha Band",
            false,
            2,
            1,
            2001,
            "Rock",
            TimeSpan.FromMilliseconds(1234.6),
            true,
            true,
            true,
            _time);

        TrackDto dto = row.ToDto();

        dto.Should().BeEquivalentTo(new
        {
            row.Id,
            Title = "Closing",
            Artist = new { Id = artistId, Name = "Alpha Band" },
            Album = new
            {
                Id = albumId,
                Title = "First Album",
                Artist = new { Id = albumArtistId, Name = "Alpha Band" },
                IsCompilation = false,
            },
            TrackNumber = 2,
            DiscNumber = 1,
            Year = 2001,
            Genre = "Rock",
            DurationMilliseconds = 1235L,
            HasAudio = true,
            HasVideo = true,
            HasCover = true,
            FirstSeenAt = _time,
        });
    }

    [Test]
    public void ToDto_TrackWithoutNamesAndAlbum_HasNoReferences()
    {
        TrackRow row = new(
            Guid.NewGuid(),
            "Clip",
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            null,
            false,
            true,
            false,
            _time);

        TrackDto dto = row.ToDto();

        dto.Artist.Should().BeNull();
        dto.Album.Should().BeNull();
        dto.DurationMilliseconds.Should().BeNull();
        dto.IsFavorite.Should().BeFalse();
        dto.Rating.Should().Be(0);
    }

    [Test]
    public void ToDto_RowsOfTheProfile_CarryTheFavoriteAndTheRating()
    {
        TrackRow track = new(
            Guid.NewGuid(),
            "Ballad",
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            null,
            true,
            false,
            false,
            _time,
            true,
            3);
        AlbumRow album = new(Guid.NewGuid(), "First Album", null, null, false, null, null, 1, 0, false, _time, true);
        ArtistRow artist = new(Guid.NewGuid(), "Alpha Band", 1, 1, _time, true);

        TrackDto trackDto = track.ToDto();

        trackDto.IsFavorite.Should().BeTrue();
        trackDto.Rating.Should().Be(3);
        album.ToDto().IsFavorite.Should().BeTrue();
        artist.ToDto().IsFavorite.Should().BeTrue();
    }

    [Test]
    public void ToDto_Compilation_HasNoAlbumArtist()
    {
        AlbumRow row = new(Guid.NewGuid(), "Hits", null, null, true, 2005, "Pop", 12, 2_400_000.4, false, _time);

        AlbumDto dto = row.ToDto();

        dto.Should().BeEquivalentTo(new
        {
            Title = "Hits",
            Artist = (ArtistReferenceDto?)null,
            IsCompilation = true,
            TrackCount = 12,
            DurationMilliseconds = 2_400_000L,
            HasCover = false,
        });
    }

    [TestCase(MediaFileIssue.TagsUnreadable, ScanIssueCodeDto.FileTagsUnreadable)]
    [TestCase(MediaFileIssue.ProbeFailed, ScanIssueCodeDto.FileProbeFailed)]
    [TestCase(MediaFileIssue.Unreadable, ScanIssueCodeDto.FileUnreadable)]
    public void ToDto_FileIssue_UsesTheCodesOfTheScan(MediaFileIssue issue, ScanIssueCodeDto expected)
    {
        issue.ToDto().Should().Be(expected);
    }

    [Test]
    public void ToDto_MediaFile_MapsKindDurationAndIssue()
    {
        MediaFile file = new()
        {
            Id = Guid.NewGuid(),
            RelativePath = "Band/Album/Song.flac",
            DirectoryPath = "Band/Album",
            FileName = "Song.flac",
            BaseName = "Song",
            NormalizedBaseName = "SONG",
            Extension = "flac",
            Kind = MediaFileKind.Audio,
            Size = 1234,
            ModifiedAt = _time,
            IsPrimary = true,
            Codec = "flac",
            Bitrate = 900_000,
            SampleRate = 44100,
            BitDepth = 16,
            Channels = 2,
            IsLossless = true,
            Duration = TimeSpan.FromSeconds(2),
            HasEmbeddedPicture = true,
            IsWritable = true,
            Issue = MediaFileIssue.ProbeFailed,
            MetadataVersion = 1,
            FirstSeenAt = _time,
            ScannedAt = _time,
        };

        MediaFileDto dto = file.ToDto();

        dto.Should().BeEquivalentTo(new
        {
            Kind = MediaFileKindDto.Audio,
            RelativePath = "Band/Album/Song.flac",
            Size = 1234L,
            DurationMilliseconds = 2000L,
            IsLossless = true,
            Issue = ScanIssueCodeDto.FileProbeFailed,
        });
    }

    [Test]
    public void ToDto_Video_MapsAudioCodecAndPixelFormat()
    {
        MediaFile file = new()
        {
            Id = Guid.NewGuid(),
            RelativePath = "Band/Clip.mp4",
            DirectoryPath = "Band",
            FileName = "Clip.mp4",
            BaseName = "Clip",
            NormalizedBaseName = "CLIP",
            Extension = "mp4",
            Kind = MediaFileKind.Video,
            Size = 1234,
            ModifiedAt = _time,
            IsPrimary = true,
            Codec = "h264",
            AudioCodec = "aac",
            PixelFormat = "yuv420p",
            IsLossless = false,
            HasEmbeddedPicture = false,
            IsWritable = false,
            MetadataVersion = 2,
            FirstSeenAt = _time,
            ScannedAt = _time,
        };

        MediaFileDto dto = file.ToDto();

        (dto.Codec, dto.AudioCodec, dto.PixelFormat).Should().Be(("h264", "aac", "yuv420p"));
    }

    [Test]
    public void ToDto_SearchResult_MapsEveryGroup()
    {
        ArtistRow artist = new(Guid.NewGuid(), "Alpha Band", 1, 2, _time);
        SearchResult result = new(new Page<ArtistRow>([artist], 0, 10, 1), new([], 0, 10, 0), new([], 0, 10, 0));

        SearchResultDto dto = result.ToDto();

        dto.Artists.Items.Should().ContainSingle().Which.Name.Should().Be("Alpha Band");
        dto.Albums.Total.Should().Be(0);
        dto.Tracks.Limit.Should().Be(10);
    }

    [Test]
    public void ToEntity_AllSortKeysAndKinds_AreMapped()
    {
        Enum.GetValues<ArtistSortDto>().Select(sort => sort.ToEntity()).Should().OnlyHaveUniqueItems()
            .And.HaveCount(Enum.GetValues<ArtistSortField>().Length);
        Enum.GetValues<AlbumSortDto>().Select(sort => sort.ToEntity()).Should().OnlyHaveUniqueItems()
            .And.HaveCount(Enum.GetValues<AlbumSortField>().Length);
        Enum.GetValues<TrackSortDto>().Select(sort => sort.ToEntity()).Should().OnlyHaveUniqueItems()
            .And.HaveCount(Enum.GetValues<TrackSortField>().Length);
        Enum.GetValues<GenreSortDto>().Select(sort => sort.ToEntity()).Should().OnlyHaveUniqueItems()
            .And.HaveCount(Enum.GetValues<GenreSortField>().Length);
        Enum.GetValues<MediaFileSortDto>().Select(sort => sort.ToEntity()).Should().OnlyHaveUniqueItems()
            .And.HaveCount(Enum.GetValues<MediaFileSortField>().Length);
        Enum.GetValues<MediaFileKindDto>().Select(kind => kind.ToEntity().ToDto())
            .Should().Equal(Enum.GetValues<MediaFileKindDto>());
        Enum.GetValues<TrackSortDto>().Select(sort => sort.ToEntity().ToString())
            .Should().Equal(Enum.GetNames<TrackSortDto>());
    }
}
