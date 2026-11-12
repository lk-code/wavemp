using AwesomeAssertions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Tests.Fakes;

namespace WaveMP.Core.Transcoding.Tests.Profiles;

[TestFixture]
public class TranscodeProfilesTests
{
    [TestCase(1920, 1080, 1080, 1920, 1080)]
    [TestCase(1920, 1080, 720, 1280, 720)]
    [TestCase(3840, 2160, 1080, 1920, 1080)]
    [TestCase(640, 360, 1080, 640, 360)]
    [TestCase(641, 361, 1080, 640, 360)]
    [TestCase(1440, 1080, 480, 640, 480)]
    [TestCase(720, 1280, 720, 406, 720)]
    public void OutputSize_Source_IsNeverUpscaledAndEven(
        int width,
        int height,
        int maxHeight,
        int expectedWidth,
        int expectedHeight)
    {
        OutputSize.Of(width, height, maxHeight).Should().Be(new OutputSize(expectedWidth, expectedHeight));
    }

    [TestCase(null, 1080)]
    [TestCase(1920, null)]
    [TestCase(0, 1080)]
    public void OutputSize_UnknownSource_IsNull(int? width, int? height)
    {
        OutputSize.Of(width, height, 1080).Should().BeNull();
    }

    [TestCase(360, 1500)]
    [TestCase(480, 1500)]
    [TestCase(720, 3500)]
    [TestCase(1080, 6000)]
    [TestCase(1440, 10000)]
    [TestCase(2160, 16000)]
    public void VideoBitrateKbpsFor_Height_FollowsTheTable(int height, int expected)
    {
        TranscodeProfiles.VideoBitrateKbpsFor(height).Should().Be(expected);
    }

    [Test]
    public void For_Video_ScalesToTheSettingsAndChoosesTheBitrate()
    {
        TranscodeInput input = TestVariants.Input(width: 3840, height: 2160);

        TranscodeProfile profile = TranscodeProfiles.For(
            HlsMedia.Video,
            input,
            new TranscodingSettings { MaxVideoHeight = 720, AudioBitrateKbps = 320 },
            TimeSpan.FromSeconds(6));

        profile.Should().Be(new TranscodeProfile(
            HlsMedia.Video,
            new OutputSize(1280, 720),
            720,
            3500,
            TranscodeProfiles.VideoAudioBitrateKbps,
            TimeSpan.FromSeconds(6)));
    }

    [Test]
    public void For_VideoOfUnknownSize_UsesTheBitrateOfTheLargestHeight()
    {
        TranscodeProfile profile = TranscodeProfiles.For(
            HlsMedia.Video,
            TestVariants.Input(width: null, height: null),
            new TranscodingSettings(),
            TimeSpan.FromSeconds(6));

        profile.Size.Should().BeNull();
        profile.VideoBitrateKbps.Should().Be(6000);
    }

    [Test]
    public void For_Audio_UsesTheAudioBitrateOfTheSettings()
    {
        TranscodeProfile profile = TranscodeProfiles.For(
            HlsMedia.Audio,
            TestVariants.Input(),
            new TranscodingSettings { AudioBitrateKbps = 160 },
            TimeSpan.FromSeconds(6));

        profile.Media.Should().Be(HlsMedia.Audio);
        profile.Size.Should().BeNull();
        profile.VideoBitrateKbps.Should().Be(0);
        profile.AudioBitrateKbps.Should().Be(160);
    }

    [TestCase(30.0, 2.0, 15)]
    [TestCase(7.0, 2.0, 4)]
    [TestCase(6.3, 2.0, 3)]
    [TestCase(6.6, 2.0, 4)]
    [TestCase(0.2, 2.0, 1)]
    [TestCase(240.0, 6.0, 40)]
    public void SegmentCount_Duration_LeavesOutShortRests(double seconds, double segment, int expected)
    {
        TranscodeVariant.CountOf(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(segment)).Should().Be(expected);
    }

    [Test]
    public void DurationOf_LastSegment_ReachesTheEnd()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(TimeSpan.FromSeconds(7)));

        variant.SegmentCount.Should().Be(4);
        variant.DurationOf(0).Should().Be(TimeSpan.FromSeconds(2));
        variant.DurationOf(3).Should().Be(TimeSpan.FromSeconds(1));
        variant.StartOf(3).Should().Be(TimeSpan.FromSeconds(6));
        variant.SegmentNameOf(3).Should().Be(variant.Id + "/00003.ts");
        FluentActions.Invoking(() => variant.DurationOf(4)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(1080, "avc1.64002a,mp4a.40.2")]
    [TestCase(1440, "avc1.640032,mp4a.40.2")]
    [TestCase(2160, "avc1.640033,mp4a.40.2")]
    public void Codecs_Video_NamesTheLevelOfTheHeight(int maxHeight, string expected)
    {
        TranscodeVariant variant = TestVariants.Video(
            TestVariants.Input(width: 3840, height: 2160),
            new TranscodingSettings { MaxVideoHeight = maxHeight });

        variant.Codecs.Should().Be(expected);
    }

    [Test]
    public void Key_SameInput_IsStableAndWellFormed()
    {
        string key = TestVariants.Video().Id;

        key.Should().Be(TestVariants.Video().Id);
        key.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Test]
    public void Key_DifferentFileVersionOrProfile_Differs()
    {
        TranscodeInput input = TestVariants.Input();
        string key = TestVariants.Video(input).Id;

        TestVariants.Video(input with { Length = input.Length + 1 }).Id.Should().NotBe(key);
        TestVariants.Video(input with { LastModified = input.LastModified.AddSeconds(1) }).Id.Should().NotBe(key);
        TestVariants.Video(input with { FileId = Guid.NewGuid() }).Id.Should().NotBe(key);
        TestVariants.Video(input, new TranscodingSettings { MaxVideoHeight = 720 }).Id.Should().NotBe(key);
        TestVariants.Audio(input).Id.Should().NotBe(key);
        TestVariants.Audio(input, new TranscodingSettings { AudioBitrateKbps = 128 }).Id
            .Should().NotBe(TestVariants.Audio(input).Id);
    }

    [Test]
    public void Key_OtherPathOfTheSameFile_IsTheSame()
    {
        TranscodeInput input = TestVariants.Input();

        TestVariants.Video(input with { FullPath = "/other/Song.mkv" }).Id.Should().Be(TestVariants.Video(input).Id);
    }
}
