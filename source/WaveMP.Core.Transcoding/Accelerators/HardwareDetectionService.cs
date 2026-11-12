using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// Runs the hardware detection once in the background after the start, so the start never waits for it.
/// </summary>
internal sealed class HardwareDetectionService(
    IHardwareAccelerationService hardware,
    ILogger<HardwareDetectionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await hardware.DetectAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopped during the detection.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The detection of the graphics hardware failed");
        }
    }
}
