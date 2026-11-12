using Microsoft.Extensions.Options;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Names;
using WaveMP.Core.Playlists.Options;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Playlists;

internal sealed class PlaylistService(
    IPlaylistRepository playlists,
    ITrackRepository tracks,
    IPlaylistCoverStore coverStore,
    TimeProvider timeProvider,
    IOptions<PlaylistOptions> options) : IPlaylistService
{
    public async Task<Page<PlaylistItem>> ListAsync(
        PlaylistListQuery query,
        CancellationToken cancellationToken = default)
    {
        Page<PlaylistRow> page = await playlists.ListAsync(query, cancellationToken);
        IReadOnlyList<PlaylistItem> items = await WithCoversAsync(page.Items, cancellationToken);
        return new Page<PlaylistItem>(items, page.Offset, page.Limit, page.Total);
    }

    public async Task<PlaylistItem?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        PlaylistRow? row = await playlists.FindAsync(userId, id, cancellationToken);
        return row is null ? null : (await WithCoversAsync([row], cancellationToken))[0];
    }

    public async Task<PlaylistResult> CreateAsync(
        Guid userId,
        string? name,
        IReadOnlyList<Guid> trackIds,
        CancellationToken cancellationToken = default)
    {
        if (!PlaylistNames.TryNormalize(name, out string normalized))
        {
            return PlaylistResult.Of(PlaylistStatus.InvalidName);
        }

        if (trackIds.Count > options.Value.MaxEntries)
        {
            return PlaylistResult.Of(PlaylistStatus.Full);
        }

        DateTimeOffset now = TruncateToMicroseconds(timeProvider.GetUtcNow());
        Playlist playlist = new()
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            Name = normalized,
            NormalizedName = PlaylistNames.ToNormalized(normalized),
            CreatedAt = now,
            UpdatedAt = now,
        };
        PlaylistEntryChange change = await playlists.CreateAsync(playlist, trackIds, cancellationToken);
        if (change.Status != PlaylistEntryChangeStatus.Done)
        {
            return PlaylistResult.Of(ToStatus(change.Status));
        }

        return await ResultOfAsync(userId, playlist.Id, cancellationToken);
    }

    public async Task<PlaylistResult> RenameAsync(
        Guid userId,
        Guid id,
        string? name,
        CancellationToken cancellationToken = default)
    {
        if (!PlaylistNames.TryNormalize(name, out string normalized))
        {
            return PlaylistResult.Of(PlaylistStatus.InvalidName);
        }

        DateTimeOffset now = TruncateToMicroseconds(timeProvider.GetUtcNow());
        bool renamed = await playlists.RenameAsync(
            userId,
            id,
            normalized,
            PlaylistNames.ToNormalized(normalized),
            now,
            cancellationToken);
        return renamed
            ? await ResultOfAsync(userId, id, cancellationToken)
            : PlaylistResult.Of(PlaylistStatus.NotFound);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        if (!await playlists.DeleteAsync(userId, id, cancellationToken))
        {
            return false;
        }

        coverStore.Delete(id);
        return true;
    }

    public async Task<Page<PlaylistEntryItem>?> ListEntriesAsync(
        Guid userId,
        Guid playlistId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        Page<PlaylistEntryRow>? page = await playlists.ListEntriesAsync(
            new PlaylistEntryListQuery(userId, playlistId, offset, limit),
            cancellationToken);
        if (page is null)
        {
            return null;
        }

        IReadOnlyList<PlaylistEntryItem> items = await WithTracksAsync(userId, page.Items, cancellationToken);
        return new Page<PlaylistEntryItem>(items, page.Offset, page.Limit, page.Total);
    }

    public async Task<PlaylistEntriesResult> GetEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        PlaylistEntryRow? entry = await playlists.FindEntryAsync(userId, playlistId, entryId, cancellationToken);
        if (entry is null)
        {
            return await NotFoundAsync(userId, playlistId, cancellationToken);
        }

        IReadOnlyList<PlaylistEntryItem> items = await WithTracksAsync(userId, [entry], cancellationToken);
        return items.Count == 0
            ? PlaylistEntriesResult.Of(PlaylistStatus.EntryNotFound)
            : new PlaylistEntriesResult(PlaylistStatus.Ok, items);
    }

    public async Task<PlaylistEntriesResult> AddEntriesAsync(
        Guid userId,
        Guid playlistId,
        IReadOnlyList<Guid> trackIds,
        int? position,
        CancellationToken cancellationToken = default)
    {
        PlaylistEntryChange change = await playlists.AddEntriesAsync(
            userId,
            playlistId,
            trackIds,
            position,
            options.Value.MaxEntries,
            TruncateToMicroseconds(timeProvider.GetUtcNow()),
            cancellationToken);
        if (change.Status != PlaylistEntryChangeStatus.Done)
        {
            return PlaylistEntriesResult.Of(ToStatus(change.Status), change.Max);
        }

        IReadOnlyList<PlaylistEntryItem> items = await WithTracksAsync(userId, change.Entries, cancellationToken);
        PlaylistItem? playlist = await GetAsync(userId, playlistId, cancellationToken);
        return new PlaylistEntriesResult(PlaylistStatus.Ok, items, playlist);
    }

    public async Task<PlaylistEntriesResult> MoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        int position,
        CancellationToken cancellationToken = default)
    {
        PlaylistEntryChange change = await playlists.MoveEntryAsync(
            userId,
            playlistId,
            entryId,
            position,
            TruncateToMicroseconds(timeProvider.GetUtcNow()),
            cancellationToken);
        if (change.Status != PlaylistEntryChangeStatus.Done)
        {
            return PlaylistEntriesResult.Of(ToStatus(change.Status), change.Max);
        }

        IReadOnlyList<PlaylistEntryItem> items = await WithTracksAsync(userId, change.Entries, cancellationToken);
        return items.Count == 0
            ? PlaylistEntriesResult.Of(PlaylistStatus.EntryNotFound)
            : new PlaylistEntriesResult(PlaylistStatus.Ok, items);
    }

    public async Task<PlaylistStatus> RemoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        PlaylistEntryChange change = await playlists.RemoveEntryAsync(
            userId,
            playlistId,
            entryId,
            TruncateToMicroseconds(timeProvider.GetUtcNow()),
            cancellationToken);
        return ToStatus(change.Status);
    }

    private async Task<IReadOnlyList<PlaylistItem>> WithCoversAsync(
        IReadOnlyList<PlaylistRow> rows,
        CancellationToken cancellationToken)
    {
        PlaylistOptions settings = options.Value;
        Guid[] withoutOwnCover = [.. rows.Where(row => row.CoverUpdatedAt is null).Select(row => row.Id)];
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> picked = withoutOwnCover.Length == 0
            ? new Dictionary<Guid, IReadOnlyList<Guid>>()
            : PlaylistCoverPicker.Pick(
                await playlists.ListCoverCandidatesAsync(
                    withoutOwnCover,
                    settings.CoverScanEntries,
                    cancellationToken),
                settings.CoverCount);

        return [.. rows.Select(row => new PlaylistItem(row, CoverOf(row, picked)))];
    }

    private static PlaylistCover CoverOf(PlaylistRow row, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> picked)
    {
        if (row.CoverUpdatedAt is { } coverUpdatedAt)
        {
            return new PlaylistCover(PlaylistCoverKind.Custom, PlaylistCoverVersions.Of(coverUpdatedAt), []);
        }

        return picked.TryGetValue(row.Id, out IReadOnlyList<Guid>? trackIds) && trackIds.Count > 0
            ? new PlaylistCover(PlaylistCoverKind.Tracks, null, trackIds)
            : PlaylistCover.None;
    }

    /// <summary>
    /// The entries with their tracks; a track removed by a scan in the meantime takes its entries with it (cascade),
    /// they are left out.
    /// </summary>
    private async Task<IReadOnlyList<PlaylistEntryItem>> WithTracksAsync(
        Guid userId,
        IReadOnlyList<PlaylistEntryRow> entries,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TrackRow> rows = await tracks.ListByIdsAsync(
            [.. entries.Select(entry => entry.TrackId)],
            userId,
            cancellationToken);
        var byId = rows.ToDictionary(row => row.Id);
        return [.. entries
            .Where(entry => byId.ContainsKey(entry.TrackId))
            .Select(entry => new PlaylistEntryItem(entry, byId[entry.TrackId]))];
    }

    private async Task<PlaylistResult> ResultOfAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        PlaylistItem? item = await GetAsync(userId, id, cancellationToken);
        return item is null ? PlaylistResult.Of(PlaylistStatus.NotFound) : new PlaylistResult(PlaylistStatus.Ok, item);
    }

    private async Task<PlaylistEntriesResult> NotFoundAsync(
        Guid userId,
        Guid playlistId,
        CancellationToken cancellationToken)
    {
        PlaylistRow? playlist = await playlists.FindAsync(userId, playlistId, cancellationToken);
        return PlaylistEntriesResult.Of(playlist is null ? PlaylistStatus.NotFound : PlaylistStatus.EntryNotFound);
    }

    private static PlaylistStatus ToStatus(PlaylistEntryChangeStatus status)
    {
        return status switch
        {
            PlaylistEntryChangeStatus.Done => PlaylistStatus.Ok,
            PlaylistEntryChangeStatus.PlaylistNotFound => PlaylistStatus.NotFound,
            PlaylistEntryChangeStatus.EntryNotFound => PlaylistStatus.EntryNotFound,
            PlaylistEntryChangeStatus.TrackNotFound => PlaylistStatus.TrackNotFound,
            PlaylistEntryChangeStatus.Full => PlaylistStatus.Full,
            PlaylistEntryChangeStatus.InvalidPosition => PlaylistStatus.InvalidPosition,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status."),
        };
    }

    /// <summary>
    /// PostgreSQL keeps microseconds; times are created with the precision they are read with later.
    /// </summary>
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);
    }
}
