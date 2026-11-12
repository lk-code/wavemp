using AwesomeAssertions;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Sources;

namespace WaveMP.Core.Streaming.Tests.Decisions;

[TestFixture]
public class VideoPlaybackDeciderTests
{
    private static readonly VideoCapabilities _browser = new(
        new HashSet<VideoContainer> { VideoContainer.Mp4, VideoContainer.Webm },
        new HashSet<VideoCodec> { VideoCodec.H264, VideoCodec.Vp9 },
        new HashSet<AudioCodec> { AudioCodec.Aac, AudioCodec.Opus });

    private static readonly VideoSource _h264Aac = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "/media/clip.mp4",
        VideoContainer.Mp4,
        VideoCodec.H264,
        AudioCodec.Aac,
        true,
        "yuv420p",
        1920,
        1080,
        48000,
        VideoContentTypes.Mp4,
        1000,
        DateTimeOffset.UnixEpoch,
        "tag",
        TimeSpan.FromMinutes(3),
        true);

    [Test]
    public void Decide_SupportedContainerAndCodecs_PlaysDirectly()
    {
        VideoPlaybackDecider.Decide(_h264Aac, _browser).Should().Be(VideoPlaybackDecision.DirectPlay);
    }

    [Test]
    public void Decide_VideoReadByAnOlderScan_IsTranscodedAsUnknown()
    {
        VideoPlaybackDecider.Decide(_h264Aac with { MetadataComplete = false }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.FormatUnknown));
    }

    [Test]
    public void Decide_UnknownContainer_IsTranscodedAsUnknown()
    {
        VideoPlaybackDecider.Decide(_h264Aac with { Container = null }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.FormatUnknown));
    }

    [Test]
    public void Decide_UnsupportedContainer_IsTranscoded()
    {
        VideoPlaybackDecider.Decide(_h264Aac with { Container = VideoContainer.Matroska }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.ContainerUnsupported));
    }

    [Test]
    public void Decide_UnsupportedOrUnknownVideoCodec_IsTranscoded()
    {
        VideoPlaybackDecider.Decide(_h264Aac with { VideoCodec = VideoCodec.Hevc }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.VideoCodecUnsupported));
        VideoPlaybackDecider.Decide(_h264Aac with { VideoCodec = null }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.VideoCodecUnsupported));
    }

    [TestCase("yuv420p10le")]
    [TestCase("yuv444p")]
    [TestCase(null)]
    public void Decide_H264OtherThan8Bit420_IsTranscoded(string? pixelFormat)
    {
        VideoPlaybackDecider.Decide(_h264Aac with { PixelFormat = pixelFormat }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.VideoCodecUnsupported));
    }

    [Test]
    public void Decide_Vp9With10Bit_PlaysDirectly()
    {
        VideoSource vp9 = _h264Aac with
        {
            Container = VideoContainer.Webm,
            VideoCodec = VideoCodec.Vp9,
            AudioCodec = AudioCodec.Opus,
            PixelFormat = "yuv420p10le",
        };

        VideoPlaybackDecider.Decide(vp9, _browser).Should().Be(VideoPlaybackDecision.DirectPlay);
    }

    [Test]
    public void Decide_UnsupportedOrUnknownAudioCodec_IsTranscoded()
    {
        VideoPlaybackDecider.Decide(_h264Aac with { AudioCodec = AudioCodec.Ac3 }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.AudioCodecUnsupported));
        VideoPlaybackDecider.Decide(_h264Aac with { AudioCodec = null }, _browser)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.AudioCodecUnsupported));
    }

    [Test]
    public void Decide_VideoWithoutSound_IgnoresTheAudioCodecs()
    {
        VideoSource silent = _h264Aac with { HasAudio = false, AudioCodec = null };

        VideoPlaybackDecider.Decide(silent, _browser with { AudioCodecs = new HashSet<AudioCodec>() })
            .Should().Be(VideoPlaybackDecision.DirectPlay);
    }

    [Test]
    public void Decide_ClientWithoutCapabilities_AlwaysTranscodes()
    {
        VideoPlaybackDecider.Decide(_h264Aac, VideoCapabilities.None)
            .Should().Be(VideoPlaybackDecision.Transcode(TranscodeReason.ContainerUnsupported));
    }
}
