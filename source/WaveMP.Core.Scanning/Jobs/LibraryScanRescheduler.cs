using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Schedulers;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;

namespace WaveMP.Core.Scanning.Jobs;

/// <summary>
/// Plans the periodic library scan again as soon as its interval changes; the next scan is one new interval from now.
/// </summary>
public sealed class LibraryScanRescheduler(IJobScheduler scheduler) : ISettingsChangeHandler<LibrarySettings>
{
    public async ValueTask OnSettingsChangedAsync(
        LibrarySettings previous,
        LibrarySettings current,
        CancellationToken cancellationToken)
    {
        if (previous.ScanIntervalMinutes != current.ScanIntervalMinutes)
        {
            await scheduler.RescheduleAsync(JobIds.LibraryScan, cancellationToken);
        }
    }
}
