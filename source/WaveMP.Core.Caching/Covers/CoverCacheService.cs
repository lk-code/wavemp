using WaveMP.Core.Library.Covers;

namespace WaveMP.Core.Caching.Covers;

internal sealed class CoverCacheService(ICoverService covers, ICoverImageCache cache) : ICoverCacheService
{
    public async Task<CoverDeliveryResult> GetTrackCoverAsync(
        Guid trackId,
        int? size,
        CancellationToken cancellationToken = default)
    {
        CoverCandidates candidates = await covers.GetTrackCandidatesAsync(trackId, cancellationToken);
        return await DeliverAsync(candidates, size, cancellationToken);
    }

    public async Task<CoverDeliveryResult> GetAlbumCoverAsync(
        Guid albumId,
        int? size,
        CancellationToken cancellationToken = default)
    {
        CoverCandidates candidates = await covers.GetAlbumCandidatesAsync(albumId, cancellationToken);
        return await DeliverAsync(candidates, size, cancellationToken);
    }

    private async Task<CoverDeliveryResult> DeliverAsync(
        CoverCandidates candidates,
        int? size,
        CancellationToken cancellationToken)
    {
        if (!candidates.OwnerExists)
        {
            return CoverDeliveryResult.OwnerNotFound;
        }

        CoverDelivery? delivery = await cache.DeliverAsync(candidates.Ordered, size, cancellationToken);
        return delivery is null ? CoverDeliveryResult.NotFound : CoverDeliveryResult.Found(delivery);
    }
}
