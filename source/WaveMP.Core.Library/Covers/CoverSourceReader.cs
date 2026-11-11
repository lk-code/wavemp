using Microsoft.Extensions.Logging;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Covers;

internal sealed class CoverSourceReader(
    IMediaFileLocator locator,
    IEmbeddedPictureReader pictures,
    ILogger<CoverSourceReader> logger) : ICoverSourceReader
{
    private const int BufferSize = 64 * 1024;

    public async Task<CoverImage?> OpenAsync(
        CoverCandidateRow candidate,
        CancellationToken cancellationToken = default)
    {
        if (!locator.TryGetFullPath(candidate.RelativePath, out string path))
        {
            logger.LogDebug("Skipping the cover {CoverId}: its path leaves the media folder", candidate.CoverId);
            return null;
        }

        try
        {
            CoverImage? image = candidate.Source == CoverSource.Embedded
                ? OpenEmbedded(candidate, path)
                : await OpenFileAsync(candidate, path, cancellationToken);
            if (image is null)
            {
                logger.LogDebug("Skipping the cover {CoverId}: no picture of a known format", candidate.CoverId);
            }

            return image;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Skipping the cover {CoverId}: its file cannot be read", candidate.CoverId);
            return null;
        }
    }

    private static async Task<CoverImage?> OpenFileAsync(
        CoverCandidateRow candidate,
        string path,
        CancellationToken cancellationToken)
    {
        FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            byte[] header = new byte[ImageFormats.HeaderLength];
            int read = await stream.ReadAtLeastAsync(
                header,
                header.Length,
                throwOnEndOfStream: false,
                cancellationToken);
            string? contentType = ImageFormats.Detect(header.AsSpan(0, read));
            if (contentType is null)
            {
                await stream.DisposeAsync();
                return null;
            }

            stream.Position = 0;
            return new CoverImage(stream, contentType, candidate.CoverId, candidate.ModifiedAt);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    private CoverImage? OpenEmbedded(CoverCandidateRow candidate, string path)
    {
        EmbeddedPictureData? picture = pictures.ReadPicture(path);
        string? contentType = picture is null ? null : ImageFormats.Detect(picture.Data);
        return contentType is null
            ? null
            : new CoverImage(
                new MemoryStream(picture!.Data, writable: false),
                contentType,
                candidate.CoverId,
                candidate.ModifiedAt);
    }
}
