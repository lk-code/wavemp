using AwesomeAssertions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Arguments;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Tests.Fakes;

namespace WaveMP.Core.Transcoding.Tests.Arguments;

[TestFixture]
public class FfmpegHlsArgumentsTests
{
    private const string Folder = "/data/cache/tmp/job";

    [Test]
    public void Build_VideoInSoftware_EncodesH264AndAacFromTheStart()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(width: 1920, height: 1080));

        IReadOnlyList<string> arguments = Build(variant, 0, HardwareAccelerator.None);

        string text = string.Join(' ', arguments);
        text.Should().StartWith("-hide_banner -nostdin -loglevel error -y -i /media/Artist/Song.mkv");
        text.Should().Contain("-map 0:v:0 -map 0:a:0? -vf scale=1920:1080,format=yuv420p");
        text.Should().Contain("-c:v libx264 -preset veryfast -profile:v high -b:v 6000k -maxrate 9000k -bufsize 12000k");
        text.Should().Contain("-force_key_frames expr:gte(t,n_forced*2)");
        text.Should().Contain("-c:a aac -b:a 192k -ac 2 -ar 48000");
        text.Should().Contain("-f hls -hls_time 2 -hls_list_size 0 -hls_segment_type mpegts");
        text.Should().Contain("-start_number 0");
        text.Should().NotContain("-ss").And.NotContain("-output_ts_offset").And.NotContain("-init_hw_device");
        arguments[^3].Should().Be("-hls_segment_filename");
        arguments[^2].Should().Be(Path.Combine(Folder, "seg-%05d.ts"));
        arguments[^1].Should().Be(Path.Combine(Folder, "ffmpeg.m3u8"));
    }

    [Test]
    public void Build_LaterSegment_SeeksAndShiftsTheTimestamps()
    {
        TranscodeVariant variant = TestVariants.Video();

        IReadOnlyList<string> arguments = Build(variant, 6, HardwareAccelerator.None);

        string text = string.Join(' ', arguments);
        text.Should().Contain("-ss 12 -i /media/Artist/Song.mkv");
        text.Should().Contain("-output_ts_offset 12 -f hls");
        text.Should().Contain("-start_number 6");
    }

    [Test]
    public void Build_FractionalSegmentLength_UsesTheInvariantCulture()
    {
        TranscodeInput input = TestVariants.Input();
        var variant = TranscodeVariant.Of(
            input,
            TranscodeProfiles.For(HlsMedia.Video, input, new TranscodingSettings(), TimeSpan.FromSeconds(1.5)));

        string text = string.Join(' ', Build(variant, 3, HardwareAccelerator.None));

        text.Should().Contain("-ss 4.5 ").And.Contain("-hls_time 1.5 ");
    }

    [Test]
    public void Build_Vaapi_OpensTheDeviceAndUploadsTheFrames()
    {
        string text = string.Join(' ', Build(TestVariants.Video(), 0, HardwareAccelerator.Vaapi));

        text.Should().Contain(
            "-init_hw_device vaapi=va:/dev/dri/renderD128 -filter_hw_device va -i /media/Artist/Song.mkv");
        text.Should().Contain("-vf scale=1920:1080,format=nv12,hwupload -c:v h264_vaapi -profile:v high");
        text.Should().NotContain("-preset");
    }

    [Test]
    public void Build_QuickSync_DerivesTheDeviceFromVaapi()
    {
        string text = string.Join(' ', Build(TestVariants.Video(), 0, HardwareAccelerator.Qsv));

        text.Should().Contain(
            "-init_hw_device vaapi=va:/dev/dri/renderD128 -init_hw_device qsv=qs@va -filter_hw_device qs");
        text.Should().Contain("format=nv12,hwupload=extra_hw_frames=64 -c:v h264_qsv");
    }

    [Test]
    public void Build_Audio_EncodesOnlyTheSoundInSoftware()
    {
        TranscodeVariant variant = TestVariants.Audio(settings: new TranscodingSettings { AudioBitrateKbps = 320 });

        string text = string.Join(' ', Build(variant, 0, HardwareAccelerator.Qsv));

        text.Should().Contain("-map 0:a:0 -vn -sn -dn -map_metadata -1 -c:a aac -b:a 320k -ac 2 -ar 48000");
        text.Should().NotContain("-c:v").And.NotContain("-init_hw_device").And.NotContain("-vf");
    }

    [Test]
    public void VideoFilter_UnknownSize_LimitsTheHeightToAnEvenValue()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(width: null, height: null));

        FfmpegHlsArguments.VideoFilter(variant.Profile, HardwareAccelerator.None)
            .Should().Be("scale=-2:'trunc(min(ih,1080)/2)*2',format=yuv420p");
    }

    [TestCase("seg-00012.ts", 12)]
    [TestCase("seg-00000.ts", 0)]
    [TestCase("/tmp/job/seg-00003.ts", 3)]
    [TestCase("seg-00012.ts.tmp", null)]
    [TestCase("seg-.ts", null)]
    [TestCase("other-00001.ts", null)]
    [TestCase("seg-0001", null)]
    public void SegmentIndexOf_FileName_ReturnsTheIndexOfSegmentFiles(string fileName, int? expected)
    {
        FfmpegHlsArguments.SegmentIndexOf(fileName).Should().Be(expected);
    }

    [Test]
    public void TestEncode_Vaapi_EncodesATestPictureWithoutOutput()
    {
        string text = string.Join(
            ' ',
            FfmpegHardwareArguments.TestEncode(HardwareAccelerator.Vaapi, "/dev/dri/renderD128"));

        text.Should().Be(
            "-hide_banner -nostdin -loglevel error -init_hw_device vaapi=va:/dev/dri/renderD128 -filter_hw_device va "
            + "-f lavfi -i testsrc2=size=320x240:rate=25:duration=1 -vf format=nv12,hwupload -c:v h264_vaapi -f null -");
    }

    private static IReadOnlyList<string> Build(
        TranscodeVariant variant,
        int startSegment,
        HardwareAccelerator accelerator)
    {
        return FfmpegHlsArguments.Build(
            variant,
            startSegment,
            accelerator,
            "/dev/dri/renderD128",
            Folder,
            "veryfast");
    }
}
