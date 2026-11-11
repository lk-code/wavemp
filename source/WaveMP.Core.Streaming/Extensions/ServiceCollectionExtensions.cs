using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Streaming.Sources;

namespace WaveMP.Core.Streaming.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the streaming: the audio file of a track and the decision between direct play and transcoding.
    /// Needs <c>AddWaveMpData</c> and <c>AddWaveMpMetadata</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpStreaming(this IServiceCollection services)
    {
        services.AddScoped<IStreamService, StreamService>();

        return services;
    }
}
