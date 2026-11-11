using AwesomeAssertions;
using WaveMP.Core.Scanning.Groupings;
using WaveMP.Core.Scanning.Qualities;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Tests.Qualities;

[TestFixture]
public class QualityTests
{
    [Test]
    public void Audio_LosslessBeforeLossy_EvenWithLowerBitrate()
    {
        MediaFile flac = Audio("Dup.flac", lossless: true, bitrate: 900_000, bitDepth: 16);
        MediaFile mp3 = Audio("Dup.mp3", lossless: false, bitrate: 320_000);

        Best(AudioQualityComparer.Instance, mp3, flac).Should().BeSameAs(flac);
    }

    [Test]
    public void Audio_Lossy_HigherBitrateWins()
    {
        MediaFile high = Audio("Lossy.mp3", lossless: false, bitrate: 320_000);
        MediaFile low = Audio("Lossy.m4a", lossless: false, bitrate: 128_000, sampleRate: 96_000);

        Best(AudioQualityComparer.Instance, low, high).Should().BeSameAs(high);
    }

    [Test]
    public void Audio_Lossless_BitDepthThenSampleRateWin()
    {
        MediaFile hires = Audio("Hires.flac", lossless: true, bitrate: 2_000_000, bitDepth: 24, sampleRate: 96_000);
        MediaFile cd = Audio("Hires.wav", lossless: true, bitrate: 1_411_200, bitDepth: 16, sampleRate: 44_100);
        MediaFile cd48 = Audio("Hires.aiff", lossless: true, bitrate: 1_000_000, bitDepth: 16, sampleRate: 48_000);

        Best(AudioQualityComparer.Instance, cd, cd48, hires).Should().BeSameAs(hires);
        Best(AudioQualityComparer.Instance, cd, cd48).Should().BeSameAs(cd48);
    }

    [Test]
    public void Audio_DamagedFile_LosesAgainstAReadableOne()
    {
        MediaFile broken = Audio("Song.flac", lossless: true, bitrate: 900_000, issue: MediaFileIssue.ProbeFailed);
        MediaFile readable = Audio("Song.mp3", lossless: false, bitrate: 128_000);

        Best(AudioQualityComparer.Instance, broken, readable).Should().BeSameAs(readable);
    }

    [Test]
    public void Audio_EqualQuality_LargerFileThenPathDecide()
    {
        MediaFile small = Audio("b.mp3", lossless: false, bitrate: 128_000, size: 10);
        MediaFile large = Audio("c.mp3", lossless: false, bitrate: 128_000, size: 20);
        MediaFile sameAsLarge = Audio("a.mp3", lossless: false, bitrate: 128_000, size: 20);

        Best(AudioQualityComparer.Instance, small, large).Should().BeSameAs(large);
        Best(AudioQualityComparer.Instance, large, sameAsLarge).Should().BeSameAs(sameAsLarge);
    }

    [Test]
    public void Video_HigherResolutionWins_ThenBitrate()
    {
        MediaFile small = Video("Clip.mp4", 320, 240, bitrate: 5_000_000);
        MediaFile large = Video("Clip.mkv", 640, 480, bitrate: 1_000_000);
        MediaFile largeFaster = Video("Clip.webm", 640, 480, bitrate: 2_000_000);

        Best(VideoQualityComparer.Instance, small, large).Should().BeSameAs(large);
        Best(VideoQualityComparer.Instance, large, largeFaster).Should().BeSameAs(largeFaster);
    }

    [Test]
    public void Image_MorePixelsWin_ThenSize()
    {
        MediaFile small = Image("Art.jpg", 100, 100, size: 900);
        MediaFile large = Image("Art.png", 300, 300, size: 100);

        Best(ImageQualityComparer.Instance, small, large).Should().BeSameAs(large);
    }

    [Test]
    public void FileGrouper_SameBaseNameInOtherCase_IsOneGroup()
    {
        IReadOnlyList<FileGroup> groups = FileGrouper.Group(
        [
            TestFiles.Create("A/Song.MP3"),
            TestFiles.Create("A/song.jpg"),
            TestFiles.Create("A/Song.mkv"),
            TestFiles.Create("A/cover.jpg"),
        ]);

        groups.Select(group => (group.NormalizedBaseName, group.Files.Count, group.IsTrack))
            .Should().Equal(("COVER", 1, false), ("SONG", 3, true));
        groups[1].BaseName.Should().Be("Song");
    }

    private static MediaFile Best(IComparer<MediaFile> comparer, params MediaFile[] files)
    {
        return files.Order(comparer).First();
    }

    private static MediaFile Audio(
        string path,
        bool lossless,
        int bitrate,
        int? bitDepth = null,
        int sampleRate = 44_100,
        long size = 1000,
        MediaFileIssue? issue = null)
    {
        return TestFiles.Create(path, MediaFileKind.Audio, file =>
        {
            file.IsLossless = lossless;
            file.Bitrate = bitrate;
            file.BitDepth = bitDepth;
            file.SampleRate = sampleRate;
            file.Size = size;
            file.Issue = issue;
        });
    }

    private static MediaFile Video(string path, int width, int height, int bitrate)
    {
        return TestFiles.Create(path, MediaFileKind.Video, file =>
        {
            file.Width = width;
            file.Height = height;
            file.Bitrate = bitrate;
        });
    }

    private static MediaFile Image(string path, int width, int height, long size)
    {
        return TestFiles.Create(path, MediaFileKind.Image, file =>
        {
            file.Width = width;
            file.Height = height;
            file.Size = size;
        });
    }
}
