using Microsoft.Extensions.Logging;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Covers;

internal sealed class CoverService(
    ICoverRepository covers,
    ISettingsSnapshot<LibrarySettings> settings,
    IMediaFileLocator locator,
    IEmbeddedPictureReader pictures,
    ILogger<CoverService> logger) : ICoverService
{
    private const int BufferSize = 64 * 1024;

    public async Task<CoverResult> GetTrackCoverAsync(Guid trackId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CoverCandidateRow>? candidates = await covers.GetTrackCandidatesAsync(trackId, cancellationToken);
        if (candidates is null)
        {
            return CoverResult.OwnerNotFound;
        }

        // Albums and tracks have no rule of their own yet.
        CoverPriority priority = CoverRules.Effective(settings.Current.CoverPriority, album: null, track: null);
        return await OpenFirstAsync(CoverResolver.OrderForTrack(trackId, priority, candidates), cancellationToken);
    }

    public async Task<CoverResult> GetAlbumCoverAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CoverCandidateRow>? candidates = await covers.GetAlbumCandidatesAsync(albumId, cancellationToken);
        if (candidates is null)
        {
            return CoverResult.OwnerNotFound;
        }

        CoverPriority priority = CoverRules.Effective(settings.Current.CoverPriority, album: null, track: null);
        return await OpenFirstAsync(CoverResolver.OrderForAlbum(priority, candidates), cancellationToken);
    }

    private async Task<CoverResult> OpenFirstAsync(
        IEnumerable<CoverCandidateRow> candidates,
        CancellationToken cancellationToken)
    {
        foreach (CoverCandidateRow candidate in candidates)
        {
            CoverImage? image = await TryOpenAsync(candidate, cancellationToken);
            if (image is not null)
            {
                return CoverResult.Found(image);
            }
        }

        return CoverResult.NotFound;
    }

    private async Task<CoverImage?> TryOpenAsync(CoverCandidateRow candidate, CancellationToken cancellationToken)
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
