using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Playlists.Covers;

namespace WaveMP.Core.Playlists.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates the folder of the playlist covers, removes unfinished files and the covers of playlists that no longer
    /// exist (e.g. of deleted users). Runs on start, after the migrations. A folder that cannot be written does not
    /// stop the start: uploads are refused, and the reason is logged.
    /// </summary>
    public static async Task InitializeWaveMpPlaylistsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await services.GetRequiredService<IPlaylistCoverStore>().InitializeAsync(cancellationToken);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPlaylistCoverService>().RemoveOrphansAsync(cancellationToken);
    }
}
