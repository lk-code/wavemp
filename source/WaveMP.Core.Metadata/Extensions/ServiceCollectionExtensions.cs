using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Options;
using WaveMP.Core.Metadata.Probes;
using WaveMP.Core.Metadata.Readers;
using WaveMP.Core.Metadata.Tags;

namespace WaveMP.Core.Metadata.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the media folder and the access to its files: the media folder comes from <c>MEDIA_PATH</c> (default
    /// <c>/media</c>; relative paths against the content root), the metadata reader reads tags with TagLib# and
    /// technical information with ffprobe (configuration section <c>Metadata</c>), and checks whether files are
    /// writable. Also locates files of the media folder by their relative path and reads embedded pictures.
    /// </summary>
    public static IServiceCollection AddWaveMpMetadata(this IServiceCollection services)
    {
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
        services.AddOptions<MetadataOptions>().BindConfiguration(MetadataOptions.SectionName);

        services.AddSingleton<ITagReader, TagLibTagReader>();
        services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        services.AddSingleton<IFileWriteAccess, FileWriteAccess>();
        services.AddSingleton<IMediaMetadataReader, MediaMetadataReader>();
        services.AddSingleton<IMediaFileLocator, MediaFileLocator>();
        services.AddSingleton<IEmbeddedPictureReader, TagLibEmbeddedPictureReader>();

        return services;
    }
}
