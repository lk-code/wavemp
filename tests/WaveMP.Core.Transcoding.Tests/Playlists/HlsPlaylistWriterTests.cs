using System.Globalization;
using AwesomeAssertions;
using WaveMP.Core.Transcoding.Playlists;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Tests.Fakes;

namespace WaveMP.Core.Transcoding.Tests.Playlists;

[TestFixture]
public class HlsPlaylistWriterTests
{
    [Test]
    public void WriteMaster_Video_DescribesTheVariant()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(width: 1280, height: 720));

        string playlist = HlsPlaylistWriter.WriteMaster(variant, "video/abc/main.m3u8?sig=1");

        playlist.Should().Be(
            "#EXTM3U\n"
            + "#EXT-X-VERSION:3\n"
            + "#EXT-X-INDEPENDENT-SEGMENTS\n"
            + "#EXT-X-STREAM-INF:BANDWIDTH=5986200,CODECS=\"avc1.64002a,mp4a.40.2\",RESOLUTION=1280x720\n"
            + "video/abc/main.m3u8?sig=1\n");
    }

    [Test]
    public void WriteMaster_Audio_HasNoResolution()
    {
        TranscodeVariant variant = TestVariants.Audio();

        string playlist = HlsPlaylistWriter.WriteMaster(variant, "audio/abc/main.m3u8");

        playlist.Should().Contain("#EXT-X-STREAM-INF:BANDWIDTH=281600,CODECS=\"mp4a.40.2\"\n");
        playlist.Should().NotContain("RESOLUTION");
    }

    [Test]
    public void WriteMaster_VideoWithoutSound_ListsOnlyTheVideoCodec()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(width: 640, height: 360, hasAudio: false));

        HlsPlaylistWriter.WriteMaster(variant, "x").Should().Contain("CODECS=\"avc1.64002a\"");
    }

    [Test]
    public void WriteMedia_SevenSeconds_ListsAllSegmentsOfTheFile()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(TimeSpan.FromSeconds(7)));

        string playlist = HlsPlaylistWriter.WriteMedia(variant, index => $"segments/{index}?sig=1");

        playlist.Should().Be(
            "#EXTM3U\n"
            + "#EXT-X-VERSION:3\n"
            + "#EXT-X-PLAYLIST-TYPE:VOD\n"
            + "#EXT-X-INDEPENDENT-SEGMENTS\n"
            + "#EXT-X-TARGETDURATION:2\n"
            + "#EXT-X-MEDIA-SEQUENCE:0\n"
            + "#EXTINF:2.000,\nsegments/0?sig=1\n"
            + "#EXTINF:2.000,\nsegments/1?sig=1\n"
            + "#EXTINF:2.000,\nsegments/2?sig=1\n"
            + "#EXTINF:1.000,\nsegments/3?sig=1\n"
            + "#EXT-X-ENDLIST\n");
    }

    [Test]
    public void WriteMedia_ShortRestAtTheEnd_AddsItToTheLastSegment()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(TimeSpan.FromSeconds(6.3)));

        string playlist = HlsPlaylistWriter.WriteMedia(variant, index => index.ToString(CultureInfo.InvariantCulture));

        playlist.Should().Contain("#EXT-X-TARGETDURATION:3\n");
        playlist.Should().EndWith("#EXTINF:2.300,\n2\n#EXT-X-ENDLIST\n");
    }

    [Test]
    public void WriteMedia_GermanCulture_WritesInvariantNumbers()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            TranscodeVariant variant = TestVariants.Video(TestVariants.Input(TimeSpan.FromSeconds(5.25)));

            string playlist = HlsPlaylistWriter.WriteMedia(variant, index => index.ToString(CultureInfo.InvariantCulture));

            playlist.Should().Contain("#EXTINF:1.250,").And.NotContain("1,250");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void ReadSegmentIndexes_PlaylistOfFfmpeg_ReturnsTheListedSegments()
    {
        const string Playlist = "#EXTM3U\r\n#EXT-X-VERSION:3\r\n#EXTINF:2.000000,\r\nseg-00004.ts\r\n"
            + "#EXTINF:2.000000,\r\nseg-00005.ts\r\n#EXTINF:1.0,\r\nseg-0000";

        HlsPlaylistWriter.ReadSegmentIndexes(Playlist).Should().Equal(4, 5);
    }
}
