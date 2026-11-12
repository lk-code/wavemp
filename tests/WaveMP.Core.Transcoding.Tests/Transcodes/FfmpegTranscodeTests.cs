using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Processes;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Tests.Fakes;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;
using AudioFormat = WaveMP.Testing.Media.AudioFormat;

namespace WaveMP.Core.Transcoding.Tests.Transcodes;

/// <summary>
/// Transcodes synthetic media with the real ffmpeg in software.
/// </summary>
[TestFixture]
public class FfmpegTranscodeTests
{
    private const int DurationSeconds = 7;

    private TemporaryDirectory _folder = null!;
    private FolderCacheStore _cache = null!;
    private TranscodingOptions _options = null!;
    private TranscodeManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = new TemporaryDirectory();
        _cache = new FolderCacheStore(_folder.Combine("cache"));
        _options = new TranscodingOptions
        {
            FfmpegPath = FfmpegTool.FfmpegPath,
            SegmentDuration = TestVariants.SegmentDuration,
            PollInterval = TimeSpan.FromMilliseconds(50),
            SegmentWaitTimeout = TimeSpan.FromSeconds(60),
            RestartAhead = TimeSpan.FromSeconds(2),
            X264Preset = "ultrafast",
        };
        IHardwareAccelerationService hardware = Substitute.For<IHardwareAccelerationService>();
        hardware.Current.Returns(new HardwareAccelerationStatus(
            HardwareAccelerationState.Unavailable,
            HardwareAccelerator.None,
            null,
            HardwareUnavailableReason.NoDevice,
            true,
            DateTimeOffset.UnixEpoch));
        ISettingsSnapshot<TranscodingSettings> settings = Substitute.For<ISettingsSnapshot<TranscodingSettings>>();
        settings.Current.Returns(new TranscodingSettings());
        Microsoft.Extensions.Options.IOptions<TranscodingOptions> options =
            Microsoft.Extensions.Options.Options.Create(_options);
        _manager = new TranscodeManager(
            new FfmpegProcessFactory(options),
            _cache,
            hardware,
            settings,
            options,
            TimeProvider.System,
            NullLogger<TranscodeManager>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _manager.Dispose();
        _folder.Dispose();
    }

    [Test]
    public async Task GetSegmentAsync_H264Video_ProducesMpegTsWithH264AndAac()
    {
        string path = _folder.Combine("media/Song.mp4");
        await TestMediaGenerator.CreateVideoAsync(
            path,
            VideoFormat.Mp4,
            320,
            240,
            codec: TestVideoCodec.H264,
            durationSeconds: DurationSeconds);
        TranscodeVariant variant = TestVariants.Video(Input(path, 320, 240));

        Probe first = await ProbeSegmentAsync(variant, 0);
        Probe last = await ProbeSegmentAsync(variant, variant.SegmentCount - 1);

        variant.SegmentCount.Should().Be(4);
        first.Codecs.Should().BeEquivalentTo("h264", "aac");
        first.Width.Should().Be(320);
        last.Codecs.Should().BeEquivalentTo("h264", "aac");
    }

    [Test]
    public async Task GetSegmentAsync_Vp9WebmAfterASeek_StartsAtTheSegmentTime()
    {
        string path = _folder.Combine("media/Song.webm");
        await TestMediaGenerator.CreateVideoAsync(
            path,
            VideoFormat.Webm,
            320,
            240,
            codec: TestVideoCodec.Vp9,
            durationSeconds: DurationSeconds);
        TranscodeVariant variant = TestVariants.Video(Input(path, 320, 240));

        Probe first = await ProbeSegmentAsync(variant, 0);
        Probe seek = await ProbeSegmentAsync(variant, 3);

        first.Codecs.Should().BeEquivalentTo("h264", "aac");
        seek.Codecs.Should().BeEquivalentTo("h264", "aac");
        seek.StartTime.Should().BeInRange(5.9, 8.0, "the segment keeps its position in the file");
    }

    [Test]
    public async Task GetSegmentAsync_Audio_ProducesMpegTsWithAacOnly()
    {
        string path = _folder.Combine("media/Song.flac");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Flac, durationSeconds: DurationSeconds);
        TranscodeVariant variant = TestVariants.Audio(Input(path, null, null));

        Probe first = await ProbeSegmentAsync(variant, 0);

        first.Codecs.Should().BeEquivalentTo("aac");
    }

    [Test]
    public async Task GetSegmentAsync_CorruptFile_Fails()
    {
        string path = _folder.Combine("media/Broken.mkv");
        TestMediaGenerator.CreateCorruptFile(path);
        TranscodeVariant variant = TestVariants.Video(Input(path, 320, 240));

        SegmentResult result = await _manager.GetSegmentAsync(variant, 0, null);

        result.Status.Should().Be(SegmentStatus.Failed);
    }

    private static TranscodeInput Input(string path, int? width, int? height)
    {
        FileInfo file = new(path);
        return new TranscodeInput(
            Guid.NewGuid(),
            Guid.NewGuid(),
            path,
            file.Length,
            file.LastWriteTimeUtc,
            TimeSpan.FromSeconds(DurationSeconds),
            true,
            width,
            height);
    }

    private async Task<Probe> ProbeSegmentAsync(TranscodeVariant variant, int index)
    {
        SegmentResult result = await _manager.GetSegmentAsync(variant, index, null);
        result.Status.Should().Be(SegmentStatus.Ready);
        string target = _folder.Combine($"segments/{variant.Id}-{index}.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using (Stream content = result.Content!)
        await using (FileStream file = File.Create(target))
        {
            await content.CopyToAsync(file);
        }

        byte[] bytes = await File.ReadAllBytesAsync(target);
        bytes.Length.Should().BeGreaterThan(376);
        bytes[0].Should().Be(0x47, "MPEG-TS packets start with the sync byte");
        bytes[188].Should().Be(0x47);
        return await ProbeAsync(target);
    }

    private static async Task<Probe> ProbeAsync(string path)
    {
        ProcessStartInfo startInfo = new(FfmpegTool.FfprobePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in (string[])
                 [
                     "-v", "error", "-show_entries", "stream=codec_name,width:format=start_time", "-of", "json", path,
                 ])
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)!;
        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, await error);

        using var document = JsonDocument.Parse(output);
        JsonElement root = document.RootElement;
        List<string> codecs = [];
        int? width = null;
        foreach (JsonElement stream in root.GetProperty("streams").EnumerateArray())
        {
            codecs.Add(stream.GetProperty("codec_name").GetString()!);
            if (stream.TryGetProperty("width", out JsonElement value))
            {
                width = value.GetInt32();
            }
        }

        double startTime = double.Parse(
            root.GetProperty("format").GetProperty("start_time").GetString()!,
            CultureInfo.InvariantCulture);
        return new Probe(codecs, width, startTime);
    }

    private sealed record Probe(IReadOnlyList<string> Codecs, int? Width, double StartTime);
}
