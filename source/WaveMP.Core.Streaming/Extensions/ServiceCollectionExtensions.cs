using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WaveMP.Core.Streaming.Options;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Core.Streaming.Sources;

namespace WaveMP.Core.Streaming.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the streaming: the audio file of a track, the decision between direct play and transcoding and the
    /// signed media URLs, with settings from the section <c>Streaming</c>. Needs <c>AddWaveMpData</c> and
    /// <c>AddWaveMpMetadata</c>. The signing key is loaded on start by
    /// <see cref="ServiceProviderExtensions.InitializeWaveMpStreamingAsync"/>.
    /// </summary>
    public static IServiceCollection AddWaveMpStreaming(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<StreamingOptions>().BindConfiguration(StreamingOptions.SectionName);

        services.AddSingleton<MediaUrlKeyStore>();
        services.AddSingleton<IMediaUrlSigner, MediaUrlSigner>();
        services.AddSingleton<IMediaUrlIssuer, MediaUrlIssuer>();
        services.AddScoped<IStreamService, StreamService>();

        return services;
    }
}
