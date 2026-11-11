using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Streaming.Signatures;

namespace WaveMP.Core.Streaming.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Loads the key that signs the media URLs, or creates it on the first start. Runs on start, after the
    /// migrations.
    /// </summary>
    public static Task InitializeWaveMpStreamingAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return services.GetRequiredService<MediaUrlKeyStore>().InitializeAsync(cancellationToken);
    }
}
