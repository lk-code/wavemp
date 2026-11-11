using AwesomeAssertions;
using WaveMP.Core.Metadata.Codecs;

namespace WaveMP.Core.Metadata.Tests.Codecs;

[TestFixture]
public class LosslessCodecsTests
{
    [TestCase("flac", true)]
    [TestCase("FLAC", true)]
    [TestCase("alac", true)]
    [TestCase("wavpack", true)]
    [TestCase("pcm_s16le", true)]
    [TestCase("pcm_s24be", true)]
    [TestCase("dsd_lsbf", true)]
    [TestCase("mp3", false)]
    [TestCase("aac", false)]
    [TestCase("opus", false)]
    [TestCase("vorbis", false)]
    [TestCase(null, false)]
    public void IsLossless_Codec_KnowsTheLosslessCodecs(string? codec, bool expected)
    {
        LosslessCodecs.IsLossless(codec).Should().Be(expected);
    }
}
