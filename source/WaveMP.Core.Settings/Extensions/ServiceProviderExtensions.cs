using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Settings.Services;

namespace WaveMP.Core.Settings.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Loads the saved server settings. Runs on start, after the migrations and before the jobs are planned.
    /// </summary>
    public static async Task InitializeWaveMpSettingsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ILibrarySettingsService>().InitializeAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ICacheSettingsService>().InitializeAsync(cancellationToken);
    }
}
