using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Extensions;
using WaveMP.Core.Scanning.Edits;
using WaveMP.Core.Scanning.Jobs;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Core.Scanning.Watchers;
using WaveMP.Core.Scanning.Writers;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;

namespace WaveMP.Core.Scanning.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the library scan, with settings from the section <c>Scanning</c>. Needs <c>AddWaveMpData</c>,
    /// <c>AddWaveMpSettings</c>, <c>AddWaveMpMetadata</c> (which also provides the media folder) and
    /// <c>AddWaveMpJobs</c>. Includes the scan job, its periodic schedule, the file watcher (hosted service) and the
    /// metadata editor, which writes tags and reads the folder of the track again.
    /// </summary>
    public static IServiceCollection AddWaveMpScanning(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<ScanOptions>().BindConfiguration(ScanOptions.SectionName);

        services.AddSingleton<IScanProgressTracker, ScanProgressTracker>();
        services.AddSingleton<LibraryWriteLock>();
        services.AddScoped<LibraryWriter>();
        services.AddScoped<ILibraryScanner, LibraryScanner>();
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<ITrackMetadataEditor, TrackMetadataEditor>();

        services.AddWaveMpJob<LibraryScanJob>(JobIds.LibraryScan);
        services.AddSingleton<IJobScheduleProvider, LibraryScanScheduleProvider>();
        services.AddSingleton<ISettingsChangeHandler<LibrarySettings>, LibraryScanRescheduler>();

        services.AddSingleton<MediaFolderWatcher>();
        services.AddHostedService(provider => provider.GetRequiredService<MediaFolderWatcher>());
        services.AddSingleton<ISettingsChangeHandler<LibrarySettings>>(provider =>
            provider.GetRequiredService<MediaFolderWatcher>());

        return services;
    }
}
