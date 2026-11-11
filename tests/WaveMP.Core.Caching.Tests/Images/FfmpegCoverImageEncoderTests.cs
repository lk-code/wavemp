using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WaveMP.Core.Caching.Images;
using WaveMP.Core.Caching.Options;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Core.Caching.Tests.Images;

/// <summary>
/// Converts synthetic pictures with the real ffmpeg.
/// </summary>
[TestFixture]
[Category("Media")]
public class FfmpegCoverImageEncoderTests
{
    private TemporaryDirectory _folder = null!;
    private FfmpegCoverImageEncoder _encoder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = new TemporaryDirectory();
        _encoder = CreateEncoder(FfmpegTool.FfmpegPath);
    }

    [TearDown]
    public void TearDown()
    {
        _encoder.Dispose();
        _folder.Dispose();
    }

    [Test]
    public async Task EncodeAsync_LargeJpeg_WritesEverySizeAsWebPInOneRun()
    {
        string source = _folder.Combine("cover.jpg");
        await TestMediaGenerator.CreateImageAsync(source, 1200, 800);
        CoverEncodeTarget small = new(96, _folder.Combine("small.webp"));
        CoverEncodeTarget medium = new(300, _folder.Combine("medium.webp"));

        CoverEncodeStatus status = await EncodeAsync(source, "image/jpeg", small, medium);

        status.Should().Be(CoverEncodeStatus.Encoded);
        await ShouldBeWebPAsync(small.OutputPath, 96, 64);
        await ShouldBeWebPAsync(medium.OutputPath, 300, 200);
    }

    [Test]
    public async Task EncodeAsync_SmallPng_NeverEnlargesIt()
    {
        string source = _folder.Combine("cover.png");
        await TestMediaGenerator.CreateImageAsync(source, 200, 100);
        CoverEncodeTarget target = new(300, _folder.Combine("medium.webp"));

        CoverEncodeStatus status = await EncodeAsync(source, "image/png", target);

        status.Should().Be(CoverEncodeStatus.Encoded);
        await ShouldBeWebPAsync(target.OutputPath, 200, 100);
    }

    [Test]
    public async Task EncodeAsync_Gif_WritesItsFirstFrame()
    {
        string source = _folder.Combine("cover.gif");
        await TestMediaGenerator.CreateImageAsync(source, 640, 480);
        CoverEncodeTarget target = new(96, _folder.Combine("small.webp"));

        CoverEncodeStatus status = await EncodeAsync(source, "image/gif", target);

        status.Should().Be(CoverEncodeStatus.Encoded);
        await ShouldBeWebPAsync(target.OutputPath, 96, 72);
    }

    [Test]
    public async Task EncodeAsync_DamagedPicture_FailsAndLeavesNoFile()
    {
        string source = _folder.Combine("broken.jpg");
        TestMediaGenerator.CreateCorruptFile(source);
        CoverEncodeTarget target = new(96, _folder.Combine("small.webp"));

        CoverEncodeStatus status = await EncodeAsync(source, "image/jpeg", target);

        status.Should().Be(CoverEncodeStatus.Failed);
        File.Exists(target.OutputPath).Should().BeFalse();
    }

    [Test]
    public async Task EncodeAsync_UnknownFormat_Fails()
    {
        string source = _folder.Combine("cover.png");
        await TestMediaGenerator.CreateImageAsync(source, 20, 20);

        CoverEncodeStatus status = await EncodeAsync(
            source,
            "image/svg+xml",
            new CoverEncodeTarget(96, _folder.Combine("small.webp")));

        status.Should().Be(CoverEncodeStatus.Failed);
    }

    [Test]
    public async Task EncodeAsync_FfmpegMissing_IsUnavailable()
    {
        using FfmpegCoverImageEncoder missing = CreateEncoder(_folder.Combine("no-ffmpeg-here"));
        using MemoryStream source = new([1, 2, 3]);

        CoverEncodeStatus status = await missing.EncodeAsync(
            source,
            "image/png",
            [new CoverEncodeTarget(96, _folder.Combine("small.webp"))]);

        status.Should().Be(CoverEncodeStatus.Unavailable);
    }

    private static FfmpegCoverImageEncoder CreateEncoder(string ffmpegPath)
    {
        return new FfmpegCoverImageEncoder(
            Microsoft.Extensions.Options.Options.Create(new CacheOptions { FfmpegPath = ffmpegPath }),
            NullLogger<FfmpegCoverImageEncoder>.Instance);
    }

    private async Task<CoverEncodeStatus> EncodeAsync(
        string source,
        string contentType,
        params CoverEncodeTarget[] targets)
    {
        await using FileStream stream = File.OpenRead(source);
        return await _encoder.EncodeAsync(stream, contentType, targets);
    }

    private static async Task ShouldBeWebPAsync(string path, int width, int height)
    {
        byte[] content = await File.ReadAllBytesAsync(path);
        content.Take(4).Should().Equal("RIFF"u8.ToArray());
        content.Skip(8).Take(4).Should().Equal("WEBP"u8.ToArray());
        (await ImageProbe.GetSizeAsync(path)).Should().Be((width, height));
    }
}
