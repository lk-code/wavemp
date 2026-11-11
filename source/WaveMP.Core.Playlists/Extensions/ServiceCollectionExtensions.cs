using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Options;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Core.Settings.Paths;

namespace WaveMP.Core.Playlists.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the playlists of the users with their uploaded covers in the folder <c>playlists</c> of the data
    /// folder (<c>DATA_PATH</c>, default <c>/data</c>) and the recently played list, with settings from the section
    /// <c>Playlists</c>. Needs <c>AddWaveMpData</c> and <c>AddWaveMpCaching</c> (ffmpeg converts the covers). The
    /// folder is prepared on start by <see cref="ServiceProviderExtensions.InitializeWaveMpPlaylistsAsync"/>.
    /// </summary>
    public static IServiceCollection AddWaveMpPlaylists(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<PlaylistOptions>()
            .BindConfiguration(PlaylistOptions.SectionName)
            .Configure<IConfiguration, IServiceProvider>((options, configuration, provider) =>
                options.RootPath = DataPaths.Resolve(
                    configuration,
                    provider.GetService<IHostEnvironment>(),
                    PlaylistOptions.FolderName));

        services.AddSingleton<IPlaylistCoverStore, PlaylistCoverStore>();
        services.AddScoped<IPlaylistService, PlaylistService>();
        services.AddScoped<IPlaylistCoverService, PlaylistCoverService>();
        services.AddScoped<IRecentlyPlayedService, RecentlyPlayedService>();

        return services;
    }
}
