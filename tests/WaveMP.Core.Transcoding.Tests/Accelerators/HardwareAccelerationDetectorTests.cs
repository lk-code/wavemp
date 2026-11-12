using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Processes;
using WaveMP.Core.Transcoding.Tests.Fakes;

namespace WaveMP.Core.Transcoding.Tests.Accelerators;

[TestFixture]
public class HardwareAccelerationDetectorTests
{
    private const string Device = "/dev/dri/renderD128";

    private static readonly FfmpegCommandResult _success = new(true, 0, string.Empty);
    private static readonly FfmpegCommandResult _failure = new(true, 1, "Failed to initialise VAAPI connection");

    private IFfmpegCommandRunner _runner = null!;
    private IRenderDeviceLocator _devices = null!;
    private ISettingsSnapshot<TranscodingSettings> _settings = null!;
    private ManualTimeProvider _time = null!;
    private HardwareAccelerationDetector _detector = null!;

    [SetUp]
    public void SetUp()
    {
        _runner = Substitute.For<IFfmpegCommandRunner>();
        _runner.RunAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_failure);
        Answer("-version", _success);
        _devices = Substitute.For<IRenderDeviceLocator>();
        _devices.Find().Returns(Device);
        _settings = Substitute.For<ISettingsSnapshot<TranscodingSettings>>();
        _settings.Current.Returns(new TranscodingSettings());
        _time = new ManualTimeProvider(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
        _detector = new HardwareAccelerationDetector(
            _runner,
            _devices,
            _settings,
            Microsoft.Extensions.Options.Options.Create(new TranscodingOptions()),
            _time,
            NullLogger<HardwareAccelerationDetector>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _detector.Dispose();
    }

    [Test]
    public void Current_BeforeTheDetection_IsDetecting()
    {
        _detector.Current.Should().Be(HardwareAccelerationStatus.Detecting);
        _detector.Current.EncoderOfNewJobs.Should().Be(HardwareAccelerator.None);
    }

    [Test]
    public async Task DetectAsync_QuickSyncWorks_IsActiveWithQuickSync()
    {
        Answer("h264_qsv", _success);

        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.Should().Be(new HardwareAccelerationStatus(
            HardwareAccelerationState.Active,
            HardwareAccelerator.Qsv,
            Device,
            null,
            true,
            _time.GetUtcNow()));
        status.EncoderOfNewJobs.Should().Be(HardwareAccelerator.Qsv);
        _detector.Current.Should().Be(status);
    }

    [Test]
    public async Task DetectAsync_OnlyVaapiWorks_IsActiveWithVaapi()
    {
        Answer("h264_vaapi", _success);

        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.State.Should().Be(HardwareAccelerationState.Active);
        status.Accelerator.Should().Be(HardwareAccelerator.Vaapi);
        await _runner.Received(1).RunAsync(
            Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains("h264_qsv")),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DetectAsync_NoEncoderWorks_IsUnavailableBecauseTheTestFailed()
    {
        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.Should().BeEquivalentTo(new
        {
            State = HardwareAccelerationState.Unavailable,
            Accelerator = HardwareAccelerator.None,
            Device,
            Reason = (HardwareUnavailableReason?)HardwareUnavailableReason.TestFailed,
            FfmpegAvailable = true,
        });
    }

    [Test]
    public async Task DetectAsync_NoRenderDevice_IsUnavailableWithoutATest()
    {
        _devices.Find().Returns((string?)null);

        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.State.Should().Be(HardwareAccelerationState.Unavailable);
        status.Reason.Should().Be(HardwareUnavailableReason.NoDevice);
        await _runner.Received(1).RunAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DetectAsync_TurnedOff_IsDisabledWithoutATest()
    {
        Answer("h264_qsv", _success);
        _settings.Current.Returns(new TranscodingSettings { HardwareAcceleration = HardwareAccelerationMode.Off });

        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.State.Should().Be(HardwareAccelerationState.Disabled);
        status.Reason.Should().BeNull();
        status.EncoderOfNewJobs.Should().Be(HardwareAccelerator.None);
        _devices.DidNotReceive().Find();
    }

    [Test]
    public async Task DetectAsync_FfmpegMissing_IsUnavailableAndTellsSo()
    {
        Answer("-version", FfmpegCommandResult.NotStarted);

        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.State.Should().Be(HardwareAccelerationState.Unavailable);
        status.Reason.Should().Be(HardwareUnavailableReason.FfmpegMissing);
        status.FfmpegAvailable.Should().BeFalse();
    }

    [Test]
    public async Task ReportEncodeFailure_ActiveAccelerator_FallsBackToSoftware()
    {
        Answer("h264_qsv", _success);
        await _detector.DetectAsync();

        _detector.ReportEncodeFailure(HardwareAccelerator.Qsv);

        _detector.Current.State.Should().Be(HardwareAccelerationState.Failed);
        _detector.Current.Reason.Should().Be(HardwareUnavailableReason.EncodeFailed);
        _detector.Current.Accelerator.Should().Be(HardwareAccelerator.Qsv);
        _detector.Current.EncoderOfNewJobs.Should().Be(HardwareAccelerator.None);
    }

    [Test]
    public async Task ReportEncodeFailure_OtherAccelerator_KeepsTheStatus()
    {
        Answer("h264_qsv", _success);
        HardwareAccelerationStatus active = await _detector.DetectAsync();

        _detector.ReportEncodeFailure(HardwareAccelerator.Vaapi);
        _detector.ReportEncodeFailure(HardwareAccelerator.None);

        _detector.Current.Should().Be(active);
    }

    [Test]
    public async Task DetectAsync_AfterAFailure_DetectsAgain()
    {
        Answer("h264_qsv", _success);
        await _detector.DetectAsync();
        _detector.ReportEncodeFailure(HardwareAccelerator.Qsv);

        HardwareAccelerationStatus status = await _detector.DetectAsync();

        status.State.Should().Be(HardwareAccelerationState.Active);
    }

    private void Answer(string argument, FfmpegCommandResult result)
    {
        _runner.RunAsync(
                Arg.Is<IReadOnlyList<string>>(arguments => arguments.Contains(argument)),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(result);
    }
}
