using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Tests.Fakes;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Transcoding.Tests.Transcodes;

[TestFixture]
public class TranscodeManagerTests
{
    private static readonly HardwareAccelerationStatus _software = new(
        HardwareAccelerationState.Unavailable,
        HardwareAccelerator.None,
        null,
        HardwareUnavailableReason.NoDevice,
        true,
        DateTimeOffset.UnixEpoch);

    private static readonly HardwareAccelerationStatus _vaapi = new(
        HardwareAccelerationState.Active,
        HardwareAccelerator.Vaapi,
        "/dev/dri/renderD128",
        null,
        true,
        DateTimeOffset.UnixEpoch);

    private TemporaryDirectory _folder = null!;
    private FolderCacheStore _cache = null!;
    private FakeFfmpegProcesses _processes = null!;
    private IHardwareAccelerationService _hardware = null!;
    private ISettingsSnapshot<TranscodingSettings> _settings = null!;
    private TranscodingOptions _options = null!;
    private ManualTimeProvider _time = null!;
    private TranscodeManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = new TemporaryDirectory();
        _cache = new FolderCacheStore(_folder.Combine("cache"));
        _processes = new FakeFfmpegProcesses();
        _hardware = Substitute.For<IHardwareAccelerationService>();
        _hardware.Current.Returns(_software);
        _settings = Substitute.For<ISettingsSnapshot<TranscodingSettings>>();
        _settings.Current.Returns(new TranscodingSettings { MaxParallelJobs = 2 });
        _options = new TranscodingOptions
        {
            SegmentDuration = TestVariants.SegmentDuration,
            PollInterval = TimeSpan.FromMilliseconds(10),
            SegmentWaitTimeout = TimeSpan.FromSeconds(10),
            RestartAhead = TimeSpan.FromSeconds(4),
            EvictIdleAfter = TimeSpan.FromSeconds(15),
            IdleTimeout = TimeSpan.FromSeconds(60),
        };
        _time = new ManualTimeProvider(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
        _manager = new TranscodeManager(
            _processes,
            _cache,
            _hardware,
            _settings,
            Microsoft.Extensions.Options.Options.Create(_options),
            _time,
            NullLogger<TranscodeManager>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _manager.Dispose();
        _folder.Dispose();
    }

    [Test]
    public async Task GetSegmentAsync_NotCached_TranscodesAndReturnsTheSegment()
    {
        TranscodeVariant variant = TestVariants.Video();

        Task<SegmentResult> request = _manager.GetSegmentAsync(variant, 0, null);
        FakeFfmpegProcess process = _processes.Last;
        process.Produce(0);

        SegmentResult result = await request;

        await ExpectContentAsync(result, FakeFfmpegProcess.ContentOf(0));
        process.ValueOf("-start_number").Should().Be("0");
        process.ValueOf("-c:v").Should().Be("libx264");
        process.Arguments.Should().NotContain("-ss");
        _cache.Contains(CacheArea.Transcodes, variant.SegmentNameOf(0)).Should().BeTrue();
    }

    [Test]
    public async Task GetSegmentAsync_Cached_DoesNotStartFfmpeg()
    {
        TranscodeVariant variant = TestVariants.Video();
        _cache.Put(CacheArea.Transcodes, variant.SegmentNameOf(3), [9, 9]);

        SegmentResult result = await _manager.GetSegmentAsync(variant, 3, null);

        await ExpectContentAsync(result, [9, 9]);
        _processes.Started.Should().BeEmpty();
    }

    [Test]
    public async Task GetSegmentAsync_NextSegmentOfARunningTranscode_WaitsForTheSameProcess()
    {
        TranscodeVariant variant = TestVariants.Video();
        Task<SegmentResult> first = _manager.GetSegmentAsync(variant, 0, null);
        FakeFfmpegProcess process = _processes.Last;
        process.Produce(0);
        (await first).Content?.Dispose();

        Task<SegmentResult> second = _manager.GetSegmentAsync(variant, 1, null);
        process.Produce(1);

        await ExpectContentAsync(await second, FakeFfmpegProcess.ContentOf(1));
        _processes.Started.Should().ContainSingle();
    }

    [Test]
    public async Task GetSegmentAsync_SegmentFarAhead_RestartsTheTranscodeAtTheSegment()
    {
        TranscodeVariant variant = TestVariants.Video();
        Task<SegmentResult> first = _manager.GetSegmentAsync(variant, 0, null);
        FakeFfmpegProcess firstProcess = _processes.Last;
        firstProcess.Produce(0);
        (await first).Content?.Dispose();

        Task<SegmentResult> seek = _manager.GetSegmentAsync(variant, 10, null);
        FakeFfmpegProcess seekProcess = _processes.Last;
        seekProcess.Produce(10);

        await ExpectContentAsync(await seek, FakeFfmpegProcess.ContentOf(10));
        firstProcess.Killed.Should().BeTrue();
        seekProcess.ValueOf("-start_number").Should().Be("10");
        seekProcess.ValueOf("-ss").Should().Be("20");
        seekProcess.ValueOf("-output_ts_offset").Should().Be("20");
    }

    [Test]
    public async Task GetSegmentAsync_AllSlotsInUse_ReturnsBusy()
    {
        _settings.Current.Returns(new TranscodingSettings { MaxParallelJobs = 1 });
        Task<SegmentResult> running = _manager.GetSegmentAsync(TestVariants.Video(), 0, null);

        SegmentResult result = await _manager.GetSegmentAsync(TestVariants.Audio(), 0, null);

        result.Status.Should().Be(SegmentStatus.Busy);
        _processes.Started.Should().ContainSingle();
        _processes.Last.Produce(0);
        (await running).Content?.Dispose();
    }

    [Test]
    public async Task GetSegmentAsync_AllSlotsTakenByAnIdleTranscode_GivesItsSlotToTheNewOne()
    {
        _settings.Current.Returns(new TranscodingSettings { MaxParallelJobs = 1 });
        Task<SegmentResult> idle = _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        FakeFfmpegProcess idleProcess = _processes.Last;
        _time.Advance(TimeSpan.FromSeconds(20));

        Task<SegmentResult> request = _manager.GetSegmentAsync(TestVariants.Audio(), 0, null);
        _processes.Last.Produce(0);

        await ExpectContentAsync(await request, FakeFfmpegProcess.ContentOf(0));
        idleProcess.Killed.Should().BeTrue();
        (await idle).Status.Should().Be(SegmentStatus.Busy);
    }

    [Test]
    public async Task GetSegmentAsync_FfmpegFails_ReturnsFailed()
    {
        Task<SegmentResult> request = _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        _processes.Last.Exit(1);

        (await request).Status.Should().Be(SegmentStatus.Failed);
        _processes.Started.Should().ContainSingle();
    }

    [Test]
    public async Task GetSegmentAsync_HardwareFailsBeforeTheFirstSegment_RetriesInSoftware()
    {
        _hardware.Current.Returns(_vaapi);
        _hardware.When(hardware => hardware.ReportEncodeFailure(HardwareAccelerator.Vaapi))
            .Do(_ => _hardware.Current.Returns(_software));
        Task<SegmentResult> request = _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        FakeFfmpegProcess hardwareProcess = _processes.Last;
        hardwareProcess.ValueOf("-c:v").Should().Be("h264_vaapi");

        hardwareProcess.Exit(1);
        FakeFfmpegProcess softwareProcess = await WaitForProcessAsync(2);
        softwareProcess.Produce(0);

        await ExpectContentAsync(await request, FakeFfmpegProcess.ContentOf(0));
        softwareProcess.ValueOf("-c:v").Should().Be("libx264");
        _hardware.Received(1).ReportEncodeFailure(HardwareAccelerator.Vaapi);
    }

    [Test]
    public async Task GetSegmentAsync_HardwareTurnedOff_UsesSoftware()
    {
        _hardware.Current.Returns(_vaapi);
        _settings.Current.Returns(new TranscodingSettings { HardwareAcceleration = HardwareAccelerationMode.Off });

        Task<SegmentResult> request = _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        _processes.Last.Produce(0);

        (await request).Content?.Dispose();
        _processes.Last.ValueOf("-c:v").Should().Be("libx264");
    }

    [Test]
    public async Task GetSegmentAsync_SegmentNotReadyInTime_ReturnsTimeout()
    {
        _options.SegmentWaitTimeout = TimeSpan.FromMilliseconds(100);

        SegmentResult result = await _manager.GetSegmentAsync(TestVariants.Video(), 0, null);

        result.Status.Should().Be(SegmentStatus.Timeout);
        _manager.ListJobs().Should().ContainSingle();
    }

    [Test]
    public async Task GetSegmentAsync_FfmpegCannotBeStarted_ReturnsUnavailable()
    {
        _processes.FailToStart = true;

        SegmentResult result = await _manager.GetSegmentAsync(TestVariants.Video(), 0, null);

        result.Status.Should().Be(SegmentStatus.Unavailable);
        Directory.Exists(_cache.TemporaryRoot)
            .Should().BeTrue();
        Directory.EnumerateFileSystemEntries(_cache.TemporaryRoot).Should().BeEmpty();
    }

    [Test]
    public async Task GetSegmentAsync_CacheUnavailable_ReturnsUnavailable()
    {
        _cache.IsAvailable = false;

        SegmentResult result = await _manager.GetSegmentAsync(TestVariants.Video(), 0, null);

        result.Status.Should().Be(SegmentStatus.Unavailable);
        _processes.Started.Should().BeEmpty();
    }

    [Test]
    public async Task GetSegmentAsync_FfmpegEndsWithoutTheSegment_ReturnsNotFound()
    {
        Task<SegmentResult> request = _manager.GetSegmentAsync(TestVariants.Video(), 14, null);
        _processes.Last.Exit(0);

        (await request).Status.Should().Be(SegmentStatus.NotFound);
    }

    [TestCase(-1)]
    [TestCase(15)]
    public async Task GetSegmentAsync_IndexOutsideTheVariant_ReturnsNotFound(int index)
    {
        SegmentResult result = await _manager.GetSegmentAsync(TestVariants.Video(), index, null);

        result.Status.Should().Be(SegmentStatus.NotFound);
        _processes.Started.Should().BeEmpty();
    }

    [Test]
    public async Task GetSegmentAsync_TranscodeFinished_RemovesTheJobAndItsTemporaryFolder()
    {
        TranscodeVariant variant = TestVariants.Video(TestVariants.Input(TimeSpan.FromSeconds(3.9)));
        Task<SegmentResult> request = _manager.GetSegmentAsync(variant, 0, null);
        FakeFfmpegProcess process = _processes.Last;
        process.Produce(0);
        process.Produce(1);
        process.Produce(2);
        process.Exit(0);
        (await request).Content?.Dispose();

        await WaitUntilAsync(() => _manager.ListJobs().Count == 0 && !Directory.Exists(process.Folder));

        _cache.Contains(CacheArea.Transcodes, variant.SegmentNameOf(1)).Should().BeTrue();
        _cache.Contains(CacheArea.Transcodes, variant.SegmentNameOf(2))
            .Should().BeFalse("the variant has two segments, the rest belongs to the segment before");
        process.Disposed.Should().BeTrue();
    }

    [Test]
    public async Task ListJobs_RunningTranscode_DescribesIt()
    {
        var userId = Guid.NewGuid();
        TranscodeVariant variant = TestVariants.Video();
        _options.SegmentWaitTimeout = TimeSpan.FromMilliseconds(50);
        await _manager.GetSegmentAsync(variant, 4, userId);

        IReadOnlyList<TranscodeJobInfo> jobs = _manager.ListJobs();

        jobs.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            variant.Input.TrackId,
            Media = HlsMedia.Video,
            Accelerator = HardwareAccelerator.None,
            StartSegment = 4,
            CompletedSegments = 4,
            SegmentCount = 15,
            StartedAt = _time.GetUtcNow(),
            UserId = (Guid?)userId,
        });
    }

    [Test]
    public async Task Cancel_RunningTranscode_StopsIt()
    {
        _options.SegmentWaitTimeout = TimeSpan.FromMilliseconds(50);
        await _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        Guid id = _manager.ListJobs().Single().Id;

        bool cancelled = _manager.Cancel(id);

        cancelled.Should().BeTrue();
        _processes.Last.Killed.Should().BeTrue();
        _manager.ListJobs().Should().BeEmpty();
        _manager.Cancel(id).Should().BeFalse();
        _manager.Cancel(Guid.NewGuid()).Should().BeFalse();
    }

    [Test]
    public async Task StopIdle_TranscodeNotAskedForLongerThanTheIdleTime_StopsIt()
    {
        _options.SegmentWaitTimeout = TimeSpan.FromMilliseconds(50);
        await _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        _time.Advance(TimeSpan.FromSeconds(30));

        _manager.StopIdle().Should().Be(0);
        _time.Advance(TimeSpan.FromSeconds(30));
        _manager.StopIdle().Should().Be(1);

        _processes.Last.Killed.Should().BeTrue();
    }

    [Test]
    public async Task Dispose_RunningTranscodes_StopsThemAndRemovesTheirFolders()
    {
        _options.SegmentWaitTimeout = TimeSpan.FromMilliseconds(50);
        await _manager.GetSegmentAsync(TestVariants.Video(), 0, null);
        FakeFfmpegProcess process = _processes.Last;

        _manager.Dispose();

        process.Killed.Should().BeTrue();
        Directory.Exists(process.Folder).Should().BeFalse();
        (await _manager.GetSegmentAsync(TestVariants.Audio(), 0, null)).Status
            .Should().Be(SegmentStatus.Unavailable);
    }

    private async Task<FakeFfmpegProcess> WaitForProcessAsync(int count)
    {
        await WaitUntilAsync(() => _processes.Started.Count >= count);
        return _processes.Started[count - 1];
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task ExpectContentAsync(SegmentResult result, byte[] expected)
    {
        result.Status.Should().Be(SegmentStatus.Ready);
        await using Stream content = result.Content!;
        using MemoryStream copy = new();
        await content.CopyToAsync(copy);
        copy.ToArray().Should().Equal(expected);
    }
}
