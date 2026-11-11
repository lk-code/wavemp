using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class CoverSourceReaderTests
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5];
    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9, 8, 7, 6, 5, 4, 3, 2, 1];
    private static readonly DateTimeOffset _modifiedAt = new(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);

    private TemporaryDirectory _media = null!;
    private IEmbeddedPictureReader _pictures = null!;
    private CoverSourceReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _media = new TemporaryDirectory();
        _pictures = Substitute.For<IEmbeddedPictureReader>();
        IMediaFileLocator locator = Substitute.For<IMediaFileLocator>();
        locator.TryGetFullPath(Arg.Any<string>(), out _).Returns(call =>
        {
            string relative = call.ArgAt<string>(0);
            call[1] = _media.Combine(relative);
            return !relative.StartsWith("..", StringComparison.Ordinal);
        });
        _reader = new CoverSourceReader(locator, _pictures, NullLogger<CoverSourceReader>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _media.Dispose();
    }

    [Test]
    public async Task OpenAsync_PictureFile_DeliversItAsStored()
    {
        CoverCandidateRow candidate = Write(Cover("Album/cover.jpg"), _jpeg);

        CoverImage image = (await _reader.OpenAsync(candidate))!;

        image.ContentType.Should().Be("image/jpeg");
        image.CoverId.Should().Be(candidate.CoverId);
        image.SourceModifiedAt.Should().Be(_modifiedAt);
        (await ReadAsync(image)).Should().Equal(_jpeg);
    }

    [Test]
    public async Task OpenAsync_FileWithWrongExtension_DetectsTheFormatByItsContent()
    {
        CoverCandidateRow candidate = Write(Cover("Album/cover.jpg"), _png);

        CoverImage image = (await _reader.OpenAsync(candidate))!;

        image.ContentType.Should().Be("image/png");
        await image.Content.DisposeAsync();
    }

    [Test]
    public async Task OpenAsync_MissingFileUnknownFormatOrPathOutside_ReturnsNull()
    {
        CoverCandidateRow notAPicture = Write(Cover("Album/folder.jpg"), "<svg/>"u8.ToArray());

        (await _reader.OpenAsync(Cover("Album/missing.jpg"))).Should().BeNull();
        (await _reader.OpenAsync(notAPicture)).Should().BeNull();
        (await _reader.OpenAsync(Cover("../outside.jpg"))).Should().BeNull();
    }

    [Test]
    public async Task OpenAsync_EmbeddedPicture_ReadsItFromTheTags()
    {
        CoverCandidateRow embedded = Cover("Album/Song.flac") with { Source = CoverSource.Embedded };
        _pictures.ReadPicture(_media.Combine("Album/Song.flac")).Returns(new EmbeddedPictureData(_png, "image/jpeg"));

        CoverImage image = (await _reader.OpenAsync(embedded))!;

        image.ContentType.Should().Be("image/png");
        (await ReadAsync(image)).Should().Equal(_png);
    }

    [Test]
    public async Task OpenAsync_EmbeddedPictureThatCannotBeRead_ReturnsNull()
    {
        CoverCandidateRow embedded = Cover("Album/Song.mp3") with { Source = CoverSource.Embedded };

        (await _reader.OpenAsync(embedded)).Should().BeNull();
    }

    private static CoverCandidateRow Cover(string relativePath)
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
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
