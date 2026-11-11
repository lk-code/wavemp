using Microsoft.Extensions.Options;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;

namespace WaveMP.Core.Scanning.Jobs;

/// <summary>
/// The periodic library scan: every scan interval of the library settings, the first time shortly after the start
/// (<see cref="ScanOptions.StartupDelay"/>) to catch changes made while the service was stopped. The periodic scan
/// always runs, also with the file watcher.
/// </summary>
public sealed class LibraryScanScheduleProvider(
    ISettingsSnapshot<LibrarySettings> settings,
    IOptions<ScanOptions> options) : IJobScheduleProvider
{
    public string JobId => JobIds.LibraryScan;

    public JobSchedule? GetSchedule(JobScheduleReason reason)
    {
        var interval = TimeSpan.FromMinutes(settings.Current.ScanIntervalMinutes);
        return new JobSchedule(interval, reason == JobScheduleReason.Startup ? options.Value.StartupDelay : interval);
    }
}
