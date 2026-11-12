using AwesomeAssertions;
using WaveMP.Core.Metadata.Probes;

namespace WaveMP.Core.Metadata.Tests.Probes;

[TestFixture]
public class FfprobeOutputParserTests
{
    private const string Mp3WithCover = """
        {
            "streams": [
                {
                    "index": 0,
                    "codec_name": "mp3",
                    "codec_type": "audio",
                    "sample_rate": "44100",
                    "channels": 2,
                    "bits_per_sample": 0,
                    "bit_rate": "320000",
                    "disposition": { "default": 0, "attached_pic": 0 },
                    "tags": { "encoder": "LAME" }
                },
                {
                    "index": 1,
                    "codec_name": "mjpeg",
                    "codec_type": "video",
                    "width": 500,
                    "height": 400,
                    "disposition": { "default": 0, "attached_pic": 1 }
                }
            ],
            "format": {
                "format_name": "mp3",
                "duration": "2.500000",
                "bit_rate": "321000",
                "tags": { "TITLE": "Song", "artist": "Band", "track": "3/12" }
            }
        }
        """;

    [Test]
    public void Parse_Mp3WithCover_ReadsNumbersWrittenAsStrings()
    {
        MediaProbeResult result = FfprobeOutputParser.Parse(Mp3WithCover)!;

        result.FormatName.Should().Be("mp3");
        result.Duration.Should().Be(TimeSpan.FromSeconds(2.5));
        result.Bitrate.Should().Be(321000);
        result.Audio.Should().Be(new AudioStreamInfo("mp3", 320000, 44100, 2, null));
        result.Video.Should().BeNull();
        result.AttachedPicture.Should().Be(new PictureStreamInfo("mjpeg", 500, 400));
    }

    [Test]
    public void Parse_Tags_AreReadInLowerCaseWithTheContainerFirst()
    {
        MediaProbeResult result = FfprobeOutputParser.Parse(Mp3WithCover)!;

        result.Tags.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["title"] = "Song",
            ["artist"] = "Band",
            ["track"] = "3/12",
            ["encoder"] = "LAME",
        });
    }

    [Test]
    public void Parse_HiResFlac_ReadsTheBitDepthOfTheRawSamples()
    {
        const string Json = """
            {
                "streams": [
                    {
                        "codec_name": "flac",
                        "codec_type": "audio",
                        "sample_rate": "96000",
                        "channels": 2,
                        "bits_per_sample": 0,
                        "bits_per_raw_sample": "24"
                    }
                ],
                "format": { "format_name": "flac", "duration": "1.0", "bit_rate": "2500000" }
            }
            """;

        MediaProbeResult result = FfprobeOutputParser.Parse(Json)!;

        result.Audio.Should().Be(new AudioStreamInfo("flac", null, 96000, 2, 24));
        result.Bitrate.Should().Be(2500000);
    }

    [Test]
    public void Parse_VideoWithAudio_ReadsBothStreams()
    {
        const string Json = """
            {
                "streams": [
                    { "codec_name": "h264", "codec_type": "video", "width": 1920, "height": 1080, "bit_rate": "4000000", "pix_fmt": "yuv420p10le" },
                    { "codec_name": "aac", "codec_type": "audio", "sample_rate": "48000", "channels": 6, "bit_rate": "256000" }
                ],
                "format": { "format_name": "matroska,webm", "duration": "N/A" }
            }
            """;

        MediaProbeResult result = FfprobeOutputParser.Parse(Json)!;

        result.Video.Should().Be(new VideoStreamInfo("h264", 1920, 1080, 4000000, "yuv420p10le"));
        result.Audio.Should().Be(new AudioStreamInfo("aac", 256000, 48000, 6, null));
        result.Duration.Should().BeNull();
        result.Bitrate.Should().BeNull();
    }

    [Test]
    public void Parse_WithoutStreamsAndFormat_ReturnsAnEmptyResult()
    {
        MediaProbeResult result = FfprobeOutputParser.Parse("{}")!;

        result.Should().BeEquivalentTo(new MediaProbeResult(
            null,
            null,
            null,
            new Dictionary<string, string>(),
            null,
            null,
            null));
    }

    [TestCase("")]
    [TestCase("not json")]
    [TestCase("[1, 2]")]
    public void Parse_InvalidOutput_ReturnsNull(string json)
    {
        FfprobeOutputParser.Parse(json).Should().BeNull();
    }
}
