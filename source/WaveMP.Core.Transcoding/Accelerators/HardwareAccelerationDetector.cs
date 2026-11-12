using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Transcoding.Arguments;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Processes;

namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// Checks that ffmpeg starts, then (unless turned off) tests Quick Sync and VA-API with a short encode on the render
/// device. The first encoder that works is used.
/// </summary>
internal sealed class HardwareAccelerationDetector(
    IFfmpegCommandRunner runner,
    IRenderDeviceLocator devices,
    ISettingsSnapshot<TranscodingSettings> settings,
    IOptions<TranscodingOptions> options,
    TimeProvider timeProvider,
    ILogger<HardwareAccelerationDetector> logger) : IHardwareAccelerationService, IDisposable
{
    private static readonly HardwareAccelerator[] _candidates = [HardwareAccelerator.Qsv, HardwareAccelerator.Vaapi];

    private readonly SemaphoreSlim _detection = new(1, 1);
    private HardwareAccelerationStatus _current = HardwareAccelerationStatus.Detecting;

    public HardwareAccelerationStatus Current => Volatile.Read(ref _current);

    public async Task<HardwareAccelerationStatus> DetectAsync(CancellationToken cancellationToken = default)
    {
        await _detection.WaitAsync(cancellationToken);
        try
        {
            Volatile.Write(ref _current, HardwareAccelerationStatus.Detecting);
            HardwareAccelerationStatus status = await RunAsync(cancellationToken);
            Volatile.Write(ref _current, status);
            Log(status);
            return status;
        }
        finally
        {
            _detection.Release();
        }
    }

    public void ReportEncodeFailure(HardwareAccelerator accelerator)
    {
        HardwareAccelerationStatus current = Current;
        if (accelerator == HardwareAccelerator.None
            || current.State != HardwareAccelerationState.Active
            || current.Accelerator != accelerator)
        {
            return;
        }

        HardwareAccelerationStatus failed = current with
        {
            State = HardwareAccelerationState.Failed,
            Reason = HardwareUnavailableReason.EncodeFailed,
        };
        if (Interlocked.CompareExchange(ref _current, failed, current) == current)
        {
            logger.LogWarning(
                "A transcode with {Accelerator} failed; videos are encoded in software until the next detection",
                accelerator);
        }
    }

    public void Dispose()
    {
        _detection.Dispose();
    }

    private async Task<HardwareAccelerationStatus> RunAsync(CancellationToken cancellationToken)
    {
        TimeSpan timeout = options.Value.DetectionTimeout;
        FfmpegCommandResult version = await runner.RunAsync(FfmpegHardwareArguments.Version, timeout, cancellationToken);
        if (!version.Succeeded)
        {
            return Finished(
                HardwareAccelerationState.Unavailable,
                HardwareAccelerator.None,
                null,
                HardwareUnavailableReason.FfmpegMissing,
                ffmpegAvailable: false);
        }

        if (settings.Current.HardwareAcceleration == HardwareAccelerationMode.Off)
        {
            return Finished(HardwareAccelerationState.Disabled, HardwareAccelerator.None, null, null);
        }

        string? device = devices.Find();
        if (device is null)
        {
            return Finished(
                HardwareAccelerationState.Unavailable,
                HardwareAccelerator.None,
                null,
                HardwareUnavailableReason.NoDevice);
        }

        foreach (HardwareAccelerator accelerator in _candidates)
        {
            FfmpegCommandResult test = await runner.RunAsync(
                FfmpegHardwareArguments.TestEncode(accelerator, device),
                timeout,
                cancellationToken);
            if (test.Succeeded)
            {
                return Finished(HardwareAccelerationState.Active, accelerator, device, null);
            }

            logger.LogDebug(
                "The test encode with {Accelerator} on {Device} failed (exit code {ExitCode}): {Error}",
                accelerator,
                device,
                test.ExitCode,
                test.ErrorOutput);
        }

        return Finished(
            HardwareAccelerationState.Unavailable,
            HardwareAccelerator.None,
            device,
            HardwareUnavailableReason.TestFailed);
    }

    private HardwareAccelerationStatus Finished(
        HardwareAccelerationState state,
        HardwareAccelerator accelerator,
        string? device,
        HardwareUnavailableReason? reason,
        bool ffmpegAvailable = true)
    {
        return new HardwareAccelerationStatus(state, accelerator, device, reason, ffmpegAvailable, timeProvider.GetUtcNow());
    }

    private void Log(HardwareAccelerationStatus status)
    {
        switch (status.State)
        {
            case HardwareAccelerationState.Active:
                logger.LogInformation(
                    "Hardware transcoding with {Accelerator} on {Device}",
                    status.Accelerator,
                    status.Device);
                break;
            case HardwareAccelerationState.Unavailable when status.Reason == HardwareUnavailableReason.FfmpegMissing:
                logger.LogError(
                    "ffmpeg ({FfmpegPath}) cannot be started; nothing can be transcoded",
                    options.Value.FfmpegPath);
                break;
            default:
                logger.LogInformation(
                    "Software transcoding (hardware {State}, {Reason})",
                    status.State,
                    status.Reason);
                break;
        }
    }
}
