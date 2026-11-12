using Microsoft.Extensions.Options;
using WaveMP.Core.Transcoding.Options;

namespace WaveMP.Core.Transcoding.Accelerators;

internal sealed class RenderDeviceLocator(IOptions<TranscodingOptions> options) : IRenderDeviceLocator
{
    public string? Find()
    {
        TranscodingOptions settings = options.Value;
        if (!string.IsNullOrWhiteSpace(settings.RenderDevice))
        {
            return File.Exists(settings.RenderDevice) ? settings.RenderDevice : null;
        }

        try
        {
            return Directory.Exists(settings.RenderDeviceFolder)
                ? Directory.EnumerateFileSystemEntries(settings.RenderDeviceFolder, "renderD*")
                    .Order(StringComparer.Ordinal)
                    .FirstOrDefault()
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
