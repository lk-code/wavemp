using AwesomeAssertions;
using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Tests.Formats;

[TestFixture]
public class AudioFormatsTests
{
    private const string Mp4 = "mov,mp4,m4a,3gp,3g2,mj2";
    private const string Matroska = "matroska,webm";

    [TestCase("mp3", "mp3", "mp3", AudioFormat.Mp3)]
    [TestCase("flac", "flac", "flac", AudioFormat.Flac)]
    [TestCase("ogg", "flac", "oga", AudioFormat.OggFlac)]
    [TestCase("ogg", "opus", "opus", AudioFormat.OggOpus)]
    [TestCase("ogg", "vorbis", "ogg", AudioFormat.OggVorbis)]
    [TestCase("aac", "aac", "aac", AudioFormat.AdtsAac)]
    [TestCase(Mp4, "aac", "m4a", AudioFormat.Mp4Aac)]
    [TestCase(Mp4, "alac", "m4a", AudioFormat.Mp4Alac)]
    [TestCase(Matroska, "opus", "webm", AudioFormat.WebmOpus)]
    [TestCase(Matroska, "vorbis", "WEBM", AudioFormat.WebmVorbis)]
    [TestCase("wav", "pcm_s16le", "wav", AudioFormat.WavPcm)]
    [TestCase("wav", "pcm_s24le", "wav", AudioFormat.WavPcm)]
    [TestCase("wav", "pcm_f32le", "wav", AudioFormat.WavPcm)]
    [TestCase("aiff", "pcm_s16be", "aif", AudioFormat.AiffPcm)]
    [TestCase("MP3", "MP3", ".MP3", AudioFormat.Mp3)]
    public void FromFile_KnownFormat_ReturnsTheFormat(
        string format,
        string codec,
        string extension,
        AudioFormat expected)
    {
        AudioFormats.FromFile(format, codec, extension).Should().Be(expected);
    }

    [TestCase("asf", "wmav2", "wma")]
    [TestCase("ape", "ape", "ape")]
    [TestCase("wv", "wavpack", "wv")]
    [TestCase("mpc", "musepack7", "mpc")]
    [TestCase("dsf", "dsd_lsbf_planar", "dsf")]
    [TestCase(Matroska, "opus", "mka")]
    [TestCase(Matroska, "flac", "webm")]
    [TestCase("wav", "pcm_mulaw", "wav")]
    [TestCase("wav", "adpcm_ms", "wav")]
    [TestCase("ogg", "speex", "spx")]
    [TestCase(Mp4, "mp3", "m4a")]
    [TestCase("mp3", "mp2", "mp3")]
    public void FromFile_FormatClientsCannotPlay_ReturnsNull(string format, string codec, string extension)
    {
        AudioFormats.FromFile(format, codec, extension).Should().BeNull();
    }

    [TestCase(null, "mp3")]
    [TestCase("mp3", null)]
    [TestCase("", "mp3")]
    [TestCase("mp3", " ")]
    public void FromFile_WithoutProbeData_ReturnsNull(string? format, string? codec)
    {
        AudioFormats.FromFile(format, codec, "mp3").Should().BeNull();
    }

    [Test]
    public void FromFile_WithoutExtension_StillDetectsFormatsThatDoNotNeedIt()
    {
        AudioFormats.FromFile("flac", "flac", null).Should().Be(AudioFormat.Flac);
        AudioFormats.FromFile(Matroska, "opus", null).Should().BeNull();
    }

    [TestCase(AudioFormat.Mp3, "audio/mpeg")]
    [TestCase(AudioFormat.Flac, "audio/flac")]
    [TestCase(AudioFormat.OggFlac, "audio/ogg")]
    [TestCase(AudioFormat.OggOpus, "audio/ogg")]
    [TestCase(AudioFormat.OggVorbis, "audio/ogg")]
    [TestCase(AudioFormat.AdtsAac, "audio/aac")]
    [TestCase(AudioFormat.Mp4Aac, "audio/mp4")]
    [TestCase(AudioFormat.Mp4Alac, "audio/mp4")]
    [TestCase(AudioFormat.WebmOpus, "audio/webm")]
    [TestCase(AudioFormat.WebmVorbis, "audio/webm")]
    [TestCase(AudioFormat.WavPcm, "audio/wav")]
    [TestCase(AudioFormat.AiffPcm, "audio/aiff")]
    public void ContentTypeOf_EveryFormat_ReturnsItsMimeType(AudioFormat format, string expected)
    {
        AudioFormats.ContentTypeOf(format).Should().Be(expected);
    }

    [Test]
    public void ContentTypeOf_Always_CoversEveryFormat()
    {
        Enum.GetValues<AudioFormat>().Should().AllSatisfy(format =>
            AudioFormats.ContentTypeOf(format).Should().StartWith("audio/"));
    }
}
