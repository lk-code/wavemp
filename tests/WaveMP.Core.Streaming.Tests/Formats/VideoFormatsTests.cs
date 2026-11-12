using AwesomeAssertions;
using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Tests.Formats;

[TestFixture]
public class VideoFormatsTests
{
    [TestCase("mov,mp4,m4a,3gp,3g2,mj2", "mp4", VideoContainer.Mp4)]
    [TestCase("mov,mp4,m4a,3gp,3g2,mj2", ".M4V", VideoContainer.Mp4)]
    [TestCase("mov,mp4,m4a,3gp,3g2,mj2", "mov", VideoContainer.Mov)]
    [TestCase("matroska,webm", "webm", VideoContainer.Webm)]
    [TestCase("matroska,webm", "mkv", VideoContainer.Matroska)]
    public void ContainerOf_KnownContainer_ReturnsIt(string format, string extension, VideoContainer expected)
    {
        VideoFormats.ContainerOf(format, extension).Should().Be(expected);
    }

    [TestCase("avi", "avi")]
    [TestCase("mpegts", "ts")]
    [TestCase("mov,mp4,m4a,3gp,3g2,mj2", "3gp")]
    [TestCase("matroska,webm", "mka")]
    [TestCase(null, "mp4")]
    [TestCase("  ", "mp4")]
    public void ContainerOf_OtherContainer_ReturnsNull(string? format, string extension)
    {
        VideoFormats.ContainerOf(format, extension).Should().BeNull();
    }

    [TestCase("h264", VideoCodec.H264)]
    [TestCase("HEVC", VideoCodec.Hevc)]
    [TestCase("vp8", VideoCodec.Vp8)]
    [TestCase("vp9", VideoCodec.Vp9)]
    [TestCase("av1", VideoCodec.Av1)]
    public void VideoCodecOf_KnownCodec_ReturnsIt(string codec, VideoCodec expected)
    {
        VideoFormats.VideoCodecOf(codec).Should().Be(expected);
    }

    [TestCase("mpeg4")]
    [TestCase("mpeg2video")]
    [TestCase(null)]
    public void VideoCodecOf_OtherCodec_ReturnsNull(string? codec)
    {
        VideoFormats.VideoCodecOf(codec).Should().BeNull();
    }

    [TestCase("aac", AudioCodec.Aac)]
    [TestCase("mp3", AudioCodec.Mp3)]
    [TestCase("opus", AudioCodec.Opus)]
    [TestCase("vorbis", AudioCodec.Vorbis)]
    [TestCase("flac", AudioCodec.Flac)]
    [TestCase("ac3", AudioCodec.Ac3)]
    [TestCase("eac3", AudioCodec.Eac3)]
    public void AudioCodecOf_KnownCodec_ReturnsIt(string codec, AudioCodec expected)
    {
        VideoFormats.AudioCodecOf(codec).Should().Be(expected);
    }

    [TestCase("dts")]
    [TestCase("pcm_s16le")]
    [TestCase(null)]
    public void AudioCodecOf_OtherCodec_ReturnsNull(string? codec)
    {
        VideoFormats.AudioCodecOf(codec).Should().BeNull();
    }

    [TestCase("yuv420p", true)]
    [TestCase("yuvj420p", true)]
    [TestCase("yuv420p10le", false)]
    [TestCase("yuv444p", false)]
    [TestCase(null, false)]
    public void IsEightBit420_PixelFormat_ChecksDepthAndChroma(string? pixelFormat, bool expected)
    {
        VideoFormats.IsEightBit420(pixelFormat).Should().Be(expected);
    }

    [TestCase(VideoContainer.Mp4, "video/mp4")]
    [TestCase(VideoContainer.Mov, "video/quicktime")]
    [TestCase(VideoContainer.Webm, "video/webm")]
    [TestCase(VideoContainer.Matroska, "video/x-matroska")]
    [TestCase(null, "application/octet-stream")]
    public void ContentTypeOf_Container_ReturnsItsMimeType(VideoContainer? container, string expected)
    {
        VideoFormats.ContentTypeOf(container).Should().Be(expected);
    }
}
