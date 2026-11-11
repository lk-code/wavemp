using AwesomeAssertions;
using WaveMP.Core.Caching.Images;

namespace WaveMP.Core.Caching.Tests.Images;

[TestFixture]
public class FfmpegCoverArgumentsTests
{
    [TestCase("image/jpeg", "jpeg_pipe")]
    [TestCase("image/png", "png_pipe")]
    [TestCase("image/webp", "webp_pipe")]
    [TestCase("image/gif", "gif")]
    [TestCase("image/bmp", "bmp_pipe")]
    [TestCase("image/svg+xml", null)]
    public void DemuxerOf_ContentType_ChoosesTheDemuxerOfTheFormat(string contentType, string? demuxer)
    {
        FfmpegCoverArguments.DemuxerOf(contentType).Should().Be(demuxer);
    }

    [Test]
    public void Build_TwoTargets_SplitsTheInputAndWritesOneWebPPerTarget()
    {
        IReadOnlyList<string> arguments = FfmpegCoverArguments.Build(
            "jpeg_pipe",
            [new CoverEncodeTarget(96, "small.webp"), new CoverEncodeTarget(300, "medium.webp")],
            80);

        arguments.Should().Equal(
            "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
            "-f", "jpeg_pipe", "-i", "pipe:0",
            "-filter_complex",
            "[0:v]split=2[s0][s1]"
            + ";[s0]scale='min(iw,96)':'min(ih,96)':force_original_aspect_ratio=decrease:flags=lanczos[o0]"
            + ";[s1]scale='min(iw,300)':'min(ih,300)':force_original_aspect_ratio=decrease:flags=lanczos[o1]",
            "-map", "[o0]", "-frames:v", "1", "-map_metadata", "-1", "-c:v", "libwebp", "-quality", "80",
            "-f", "webp", "small.webp",
            "-map", "[o1]", "-frames:v", "1", "-map_metadata", "-1", "-c:v", "libwebp", "-quality", "80",
            "-f", "webp", "medium.webp");
    }

    [Test]
    public void Build_WithoutTargets_Throws()
    {
        Action build = () => FfmpegCoverArguments.Build("png_pipe", [], 80);

        build.Should().Throw<ArgumentOutOfRangeException>();
    }
}
