using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Core.Scanning.Writers;

namespace WaveMP.Core.Scanning.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the library scan. The media folder comes from <c>MEDIA_PATH</c> (default <c>/media</c>; relative
    /// paths against the content root), further settings from the section <c>Scanning</c>. Needs <c>AddWaveMpData</c>,
    /// <c>AddWaveMpSettings</c>, <c>AddWaveMpMetadata</c> and <c>AddWaveMpJobs</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpScanning(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<MediaLibraryOptions>()
            .Configure<IConfiguration, IServiceProvider>((options, configuration, provider) =>
            {
                string path = configuration[MediaLibraryOptions.PathVariable] is { Length: > 0 } configured
                    ? configured
                    : MediaLibraryOptions.DefaultPath;
                string contentRoot = provider.GetService<IHostEnvironment>()?.ContentRootPath
                    ?? Directory.GetCurrentDirectory();
                options.RootPath = Path.GetFullPath(path, contentRoot);
            });
        services.AddOptions<ScanOptions>().BindConfiguration(ScanOptions.SectionName);

        services.AddSingleton<IScanProgressTracker, ScanProgressTracker>();
        services.AddScoped<LibraryWriter>();
        services.AddScoped<ILibraryScanner, LibraryScanner>();

        return services;
    }
}
