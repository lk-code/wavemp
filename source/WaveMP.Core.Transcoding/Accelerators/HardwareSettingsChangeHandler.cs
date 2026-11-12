using Microsoft.Extensions.Logging;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;

namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// Detects the hardware again in the background as soon as it is turned on or off in the settings.
/// </summary>
internal sealed class HardwareSettingsChangeHandler(
    IHardwareAccelerationService hardware,
    ILogger<HardwareSettingsChangeHandler> logger) : ISettingsChangeHandler<TranscodingSettings>
{
    /// <summary>
    /// The detection started last, for tests.
    /// </summary>
    internal Task LastDetection { get; private set; } = Task.CompletedTask;

    public ValueTask OnSettingsChangedAsync(
        TranscodingSettings previous,
        TranscodingSettings current,
        CancellationToken cancellationToken)
    {
        if (previous.HardwareAcceleration != current.HardwareAcceleration)
        {
            LastDetection = Task.Run(async () =>
            {
                try
                {
                    await hardware.DetectAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "The detection of the graphics hardware failed");
                }
            }, CancellationToken.None);
        }

        return ValueTask.CompletedTask;
    }
}
