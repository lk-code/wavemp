using System.Text;
using AwesomeAssertions;
using WaveMP.Core.Library.Covers;

namespace WaveMP.Core.Library.Tests.Covers;

[TestFixture]
public class ImageFormatsTests
{
    private static IEnumerable<TestCaseData> Signatures()
    {
        yield return new TestCaseData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 }, "image/jpeg")
            .SetArgDisplayNames("JPEG");
        yield return new TestCaseData(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D },
            "image/png").SetArgDisplayNames("PNG");
        yield return new TestCaseData(Encoding.ASCII.GetBytes("GIF89a......"), "image/gif")
            .SetArgDisplayNames("GIF89a");
        yield return new TestCaseData(Encoding.ASCII.GetBytes("GIF87a"), "image/gif").SetArgDisplayNames("GIF87a");
        yield return new TestCaseData(Encoding.ASCII.GetBytes("RIFF\u0001\u0002\u0003\u0004WEBPVP8 "), "image/webp")
            .SetArgDisplayNames("WebP");
        yield return new TestCaseData(Encoding.ASCII.GetBytes("BM6\u0000"), "image/bmp").SetArgDisplayNames("BMP");
    }

    [TestCaseSource(nameof(Signatures))]
    public void Detect_KnownSignature_ReturnsTheContentType(byte[] header, string expected)
    {
        ImageFormats.Detect(header).Should().Be(expected);
        ImageFormats.ContentTypes.Should().Contain(expected);
    }

    [TestCase("<svg xmlns=")]
    [TestCase("<?xml version")]
    [TestCase("RIFF....WAVE")]
    [TestCase("ID3")]
    [TestCase("")]
    [TestCase("B")]
    public void Detect_OtherContent_ReturnsNull(string header)
    {
        ImageFormats.Detect(Encoding.ASCII.GetBytes(header)).Should().BeNull();
    }
}
