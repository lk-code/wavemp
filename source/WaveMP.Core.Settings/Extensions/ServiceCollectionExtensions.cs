using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Services;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Settings.Stores;

namespace WaveMP.Core.Settings.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the server settings: the store, the snapshots of the groups and the library settings. Needs the
    /// repositories of <c>AddWaveMpData</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpSettings(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddSingleton<SettingsSnapshot<LibrarySettings>>();
        services.AddSingleton<ISettingsSnapshot<LibrarySettings>>(provider =>
            provider.GetRequiredService<SettingsSnapshot<LibrarySettings>>());
        services.AddScoped<ILibrarySettingsService, LibrarySettingsService>();

        return services;
    }
}
