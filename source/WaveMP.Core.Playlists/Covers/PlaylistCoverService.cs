using System.Globalization;
using Microsoft.Extensions.Logging;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Covers;

internal sealed class PlaylistCoverService(
    IPlaylistRepository playlists,
    IPlaylistService service,
    IPlaylistCoverStore store,
    TimeProvider timeProvider,
    ILogger<PlaylistCoverService> logger) : IPlaylistCoverService
{
    internal const string ContentType = "image/webp";

    public async Task<PlaylistCoverResult> SetAsync(
        Guid userId,
        Guid playlistId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (await playlists.FindAsync(userId, playlistId, cancellationToken) is null)
        {
            return new PlaylistCoverResult(PlaylistCoverStatus.NotFound, null);
        }

        CoverSaveStatus saved = await store.SaveAsync(playlistId, content, cancellationToken);
        if (saved != CoverSaveStatus.Saved)
        {
            return new PlaylistCoverResult(
                saved switch
                {
                    CoverSaveStatus.UnsupportedFormat => PlaylistCoverStatus.UnsupportedFormat,
                    CoverSaveStatus.Unreadable => PlaylistCoverStatus.Unreadable,
                    _ => PlaylistCoverStatus.Unavailable,
                },
                null);
        }

        DateTimeOffset now = TruncateToMicroseconds(timeProvider.GetUtcNow());
        if (!await playlists.SetCoverAsync(userId, playlistId, now, now, cancellationToken))
        {
            // Deleted in the meantime.
            store.Delete(playlistId);
            return new PlaylistCoverResult(PlaylistCoverStatus.NotFound, null);
        }

        PlaylistItem? item = await service.GetAsync(userId, playlistId, cancellationToken);
        return item is null
            ? new PlaylistCoverResult(PlaylistCoverStatus.NotFound, null)
            : new PlaylistCoverResult(PlaylistCoverStatus.Saved, item);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid playlistId, CancellationToken cancellationToken = default)
    {
        PlaylistRow? playlist = await playlists.FindAsync(userId, playlistId, cancellationToken);
        if (playlist is null)
        {
            return false;
        }

        if (playlist.CoverUpdatedAt is not null)
        {
            DateTimeOffset now = TruncateToMicroseconds(timeProvider.GetUtcNow());
            if (!await playlists.SetCoverAsync(userId, playlistId, null, now, cancellationToken))
            {
                return false;
            }
        }

        store.Delete(playlistId);
        return true;
    }

    public async Task<CoverDeliveryResult> GetAsync(
        Guid userId,
        Guid playlistId,
        int? size,
        CancellationToken cancellationToken = default)
    {
        PlaylistRow? playlist = await playlists.FindAsync(userId, playlistId, cancellationToken);
        if (playlist is null)
        {
            return CoverDeliveryResult.OwnerNotFound;
        }

        if (playlist.CoverUpdatedAt is not { } coverUpdatedAt)
        {
            return CoverDeliveryResult.NotFound;
        }

        int snapped = CoverSizes.Snap(size ?? CoverSizes.Large);
        Stream? content = store.Open(playlistId, snapped);
        if (content is null)
        {
            logger.LogWarning("The cover of the playlist {PlaylistId} is missing in its folder", playlistId);
            return CoverDeliveryResult.NotFound;
        }

        string etag = string.Create(
            CultureInfo.InvariantCulture,
            $"{PlaylistCoverVersions.Of(coverUpdatedAt)}-{snapped}");
        return CoverDeliveryResult.Found(new CoverDelivery(content, ContentType, etag, coverUpdatedAt));
    }

    public async Task RemoveOrphansAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> stored = store.ListPlaylistIds();
        if (stored.Count == 0)
        {
            return;
        }

        IReadOnlySet<Guid> existing = await playlists.ListExistingIdsAsync(stored, cancellationToken);
        foreach (Guid id in stored.Where(id => !existing.Contains(id)))
        {
            store.Delete(id);
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);
    }
}
