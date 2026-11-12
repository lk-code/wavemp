using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Transcoding.Options;

namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// Stops transcodes no client asked for longer than the idle time, so closed players do not keep ffmpeg running.
/// </summary>
internal sealed class TranscodeJanitor(
    ITranscodeManager manager,
    IOptions<TranscodingOptions> options,
    TimeProvider timeProvider,
    ILogger<TranscodeJanitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.JanitorInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    manager.StopIdle();
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Stopping idle transcodes failed");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }
}
