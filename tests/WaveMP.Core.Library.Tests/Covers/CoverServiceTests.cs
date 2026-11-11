using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class CoverServiceTests
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5];
    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9, 8, 7, 6, 5, 4, 3, 2, 1];
    private static readonly DateTimeOffset _modifiedAt = new(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly Guid _trackId = Guid.NewGuid();
    private readonly Guid _albumId = Guid.NewGuid();

    private TemporaryDirectory _media = null!;
    private ICoverRepository _covers = null!;
    private IEmbeddedPictureReader _pictures = null!;
    private ISettingsSnapshot<LibrarySettings> _settings = null!;
    private CoverService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _media = new TemporaryDirectory();
        _covers = Substitute.For<ICoverRepository>();
        _pictures = Substitute.For<IEmbeddedPictureReader>();
        _settings = Substitute.For<ISettingsSnapshot<LibrarySettings>>();
        _settings.Current.Returns(new LibrarySettings());
        IMediaFileLocator locator = Substitute.For<IMediaFileLocator>();
        locator.TryGetFullPath(Arg.Any<string>(), out _).Returns(call =>
        {
            string relative = call.ArgAt<string>(0);
            call[1] = _media.Combine(relative);
            return !relative.StartsWith("..", StringComparison.Ordinal);
        });
        _service = new CoverService(_covers, _settings, locator, _pictures, NullLogger<CoverService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _media.Dispose();
    }

    [Test]
    public async Task GetTrackCoverAsync_UnknownTrack_ReturnsOwnerNotFound()
    {
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CoverCandidateRow>?)null);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Status.Should().Be(CoverStatus.OwnerNotFound);
        result.Image.Should().BeNull();
    }

    [Test]
    public async Task GetTrackCoverAsync_WithoutCovers_ReturnsNotFound()
    {
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([]);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Should().Be(CoverResult.NotFound);
    }

    [Test]
    public async Task GetTrackCoverAsync_AlbumAndOwnCover_DeliversTheAlbumCoverByDefault()
    {
        CoverCandidateRow album = Write(AlbumCover("Album/cover.jpg"), _jpeg);
        CoverCandidateRow own = Write(TrackCover("Album/Song.png"), _png);
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([own, album]);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Status.Should().Be(CoverStatus.Found);
        result.Image!.ContentType.Should().Be("image/jpeg");
        result.Image.CoverId.Should().Be(album.CoverId);
        result.Image.SourceModifiedAt.Should().Be(_modifiedAt);
        (await ReadAsync(result.Image)).Should().Equal(_jpeg);
    }

    [Test]
    public async Task GetTrackCoverAsync_TrackFirst_DeliversTheOwnCover()
    {
        _settings.Current.Returns(new LibrarySettings { CoverPriority = CoverPriority.TrackFirst });
        CoverCandidateRow album = Write(AlbumCover("Album/cover.jpg"), _jpeg);
        CoverCandidateRow own = Write(TrackCover("Album/Song.png"), _png);
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([album, own]);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Image!.CoverId.Should().Be(own.CoverId);
        result.Image.ContentType.Should().Be("image/png");
        await result.Image.Content.DisposeAsync();
    }

    [Test]
    public async Task GetTrackCoverAsync_MissingOrUnknownFiles_FallsBackToTheNextCover()
    {
        CoverCandidateRow missing = AlbumCover("Album/missing.jpg");
        CoverCandidateRow notAPicture = Write(
            AlbumCover("Album/folder.jpg") with { IsPrimary = false },
            "<svg/>"u8.ToArray());
        CoverCandidateRow outside = TrackCover("../outside.jpg");
        CoverCandidateRow own = Write(TrackCover("Album/Song.jpg") with { IsPrimary = false }, _jpeg);
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns([own, outside, notAPicture, missing]);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Image!.CoverId.Should().Be(own.CoverId);
        await result.Image.Content.DisposeAsync();
    }

    [Test]
    public async Task GetTrackCoverAsync_EmbeddedPicture_ReadsItFromTheTags()
    {
        CoverCandidateRow embedded = TrackCover("Album/Song.flac") with { Source = CoverSource.Embedded };
        _pictures.ReadPicture(_media.Combine("Album/Song.flac")).Returns(new EmbeddedPictureData(_png, "image/jpeg"));
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([embedded]);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Image!.ContentType.Should().Be("image/png");
        (await ReadAsync(result.Image)).Should().Equal(_png);
    }

    [Test]
    public async Task GetTrackCoverAsync_EmbeddedPictureThatCannotBeRead_ReturnsNotFound()
    {
        CoverCandidateRow embedded = TrackCover("Album/Song.mp3") with { Source = CoverSource.Embedded };
        _covers.GetTrackCandidatesAsync(_trackId, Arg.Any<CancellationToken>()).Returns([embedded]);

        CoverResult result = await _service.GetTrackCoverAsync(_trackId);

        result.Status.Should().Be(CoverStatus.NotFound);
    }

    [Test]
    public async Task GetAlbumCoverAsync_UnknownAlbum_ReturnsOwnerNotFound()
    {
        _covers.GetAlbumCandidatesAsync(_albumId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CoverCandidateRow>?)null);

        CoverResult result = await _service.GetAlbumCoverAsync(_albumId);

        result.Status.Should().Be(CoverStatus.OwnerNotFound);
    }

    [Test]
    public async Task GetAlbumCoverAsync_WithoutAlbumPicture_UsesTheCoverOfATrack()
    {
        CoverCandidateRow track = Write(TrackCover("Album/Song.png"), _png);
        _covers.GetAlbumCandidatesAsync(_albumId, Arg.Any<CancellationToken>()).Returns([track]);

        CoverResult result = await _service.GetAlbumCoverAsync(_albumId);

        result.Image!.CoverId.Should().Be(track.CoverId);
        await result.Image.Content.DisposeAsync();
    }

    private CoverCandidateRow AlbumCover(string relativePath)
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
            relativePath,
            "image/jpeg",
            100,
            _modifiedAt);
    }

    private CoverCandidateRow TrackCover(string relativePath)
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            _trackId,
            _albumId,
            1,
            1,
            "Song",
            true,
            CoverSource.File,
            relativePath,
            null,
            100,
            _modifiedAt);
    }

    private CoverCandidateRow Write(CoverCandidateRow candidate, byte[] content)
    {
        string path = _media.Combine(candidate.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return candidate;
    }

    private static async Task<byte[]> ReadAsync(CoverImage image)
    {
        await using Stream content = image.Content;
        using MemoryStream copy = new();
        await content.CopyToAsync(copy);
        return copy.ToArray();
    }
}
