using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Options;
using WaveMP.Core.Metadata.Probes;
using WaveMP.Core.Metadata.Readers;
using WaveMP.Core.Metadata.Tags;

namespace WaveMP.Core.Metadata.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the metadata reader: tags with TagLib#, technical information with ffprobe (configuration section
    /// <c>Metadata</c>) and the check whether files are writable.
    /// </summary>
    public static IServiceCollection AddWaveMpMetadata(this IServiceCollection services)
    {
        services.AddOptions<MetadataOptions>().BindConfiguration(MetadataOptions.SectionName);

        services.AddSingleton<ITagReader, TagLibTagReader>();
        services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        services.AddSingleton<IFileWriteAccess, FileWriteAccess>();
        services.AddSingleton<IMediaMetadataReader, MediaMetadataReader>();

        return services;
    }
}
