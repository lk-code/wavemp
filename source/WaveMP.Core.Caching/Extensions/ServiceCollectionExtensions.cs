using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Images;
using WaveMP.Core.Caching.Jobs;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Caching.Services;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Extensions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Paths;

namespace WaveMP.Core.Caching.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the cache: its folder <c>cache</c> in the data folder of <c>DATA_PATH</c> (default <c>/data</c>;
    /// relative paths against the content root), settings from the section <c>Caching</c>, the scaled covers with
    /// ffmpeg, the job <c>cover-cache</c> that prepares them after scans and the clean-up when the limit is lowered.
    /// Needs <c>AddWaveMpData</c>, <c>AddWaveMpSettings</c>, <c>AddWaveMpLibrary</c> and <c>AddWaveMpJobs</c>. The
    /// folder is prepared on start by <see cref="ServiceProviderExtensions.InitializeWaveMpCacheAsync"/>.
    /// </summary>
    public static IServiceCollection AddWaveMpCaching(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.SectionName)
            .Configure<IConfiguration, IServiceProvider>((options, configuration, provider) =>
                options.RootPath = DataPaths.Resolve(
                    configuration,
                    provider.GetService<IHostEnvironment>(),
                    CacheOptions.FolderName));

        services.AddSingleton<ICacheStore, CacheStore>();
        services.AddSingleton<ICoverImageEncoder, FfmpegCoverImageEncoder>();
        services.AddSingleton<ICoverImageCache, CoverImageCache>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<ICoverCacheService, CoverCacheService>();
        services.AddScoped<ICoverCachePreparer, CoverCachePreparer>();

        services.AddWaveMpJob<CoverCacheJob>(JobIds.CoverCache);
        services.AddSingleton<ISettingsChangeHandler<CacheSettings>, CacheLimitChangeHandler>();

        return services;
    }
}
