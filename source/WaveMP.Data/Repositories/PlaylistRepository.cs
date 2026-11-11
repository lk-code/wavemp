using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class PlaylistRepository(WaveMpDbContext dbContext) : IPlaylistRepository
{
    public Task<Page<PlaylistRow>> ListAsync(PlaylistListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Playlist> playlists = dbContext.Playlists.AsNoTracking()
            .Where(playlist => playlist.UserId == query.UserId);
        if (query.Search is { } search)
        {
            string pattern = LikePatterns.Contains(search);
            playlists = playlists.Where(playlist =>
                EF.Functions.ILike(playlist.Name, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.TrigramsAreWordSimilar(search, playlist.Name));
        }

        IOrderedQueryable<Playlist> sorted = query.Sort switch
        {
            PlaylistSortField.CreatedAt => playlists.OrderBy(playlist => playlist.CreatedAt, query.Direction),
            PlaylistSortField.UpdatedAt => playlists.OrderBy(playlist => playlist.UpdatedAt, query.Direction),
            _ => playlists.OrderBy(playlist => playlist.NormalizedName, query.Direction),
        };

        return Project(sorted.ThenBy(playlist => playlist.Id))
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<PlaylistRow?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        return Project(dbContext.Playlists.AsNoTracking()
                .Where(playlist => playlist.Id == id && playlist.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PlaylistEntryChange> CreateAsync(
        Playlist playlist,
        IReadOnlyList<Guid> trackIds,
        CancellationToken cancellationToken = default)
    {
        return InTransactionAsync(
            async token =>
            {
                if (!await TracksExistAsync(trackIds, token))
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.TrackNotFound);
                }

                dbContext.Playlists.Add(playlist);
                List<PlaylistEntry> entries = CreateEntries(playlist.Id, trackIds, 0, playlist.CreatedAt);
                dbContext.PlaylistEntries.AddRange(entries);
                await dbContext.SaveChangesAsync(token);
                return Done(entries);
            },
            cancellationToken);
    }

    public async Task<bool> RenameAsync(
        Guid userId,
        Guid id,
        string name,
        string normalizedName,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        int changed = await dbContext.Playlists
            .Where(playlist => playlist.Id == id && playlist.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(playlist => playlist.Name, name)
                    .SetProperty(playlist => playlist.NormalizedName, normalizedName)
                    .SetProperty(playlist => playlist.UpdatedAt, now),
                cancellationToken);
        return changed > 0;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        int deleted = await dbContext.Playlists
            .Where(playlist => playlist.Id == id && playlist.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<bool> SetCoverAsync(
        Guid userId,
        Guid id,
        DateTimeOffset? coverUpdatedAt,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        int changed = await dbContext.Playlists
            .Where(playlist => playlist.Id == id && playlist.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(playlist => playlist.CoverUpdatedAt, coverUpdatedAt)
                    .SetProperty(playlist => playlist.UpdatedAt, now),
                cancellationToken);
        return changed > 0;
    }

    public async Task<Page<PlaylistEntryRow>?> ListEntriesAsync(
        PlaylistEntryListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(query.UserId, query.PlaylistId, cancellationToken))
        {
            return null;
        }

        Page<PlaylistEntry> page = await Ordered(query.PlaylistId)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
        List<PlaylistEntryRow> rows = [.. page.Items.Select((entry, index) =>
            new PlaylistEntryRow(entry.Id, entry.TrackId, query.Offset + index, entry.AddedAt))];
        return new Page<PlaylistEntryRow>(rows, page.Offset, page.Limit, page.Total);
    }

    public async Task<PlaylistEntryRow?> FindEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        PlaylistEntry? entry = await dbContext.PlaylistEntries.AsNoTracking()
            .Where(candidate => candidate.Id == entryId
                && candidate.PlaylistId == playlistId
                && candidate.Playlist!.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is null)
        {
            return null;
        }

        int index = await IndexOfAsync(entry, cancellationToken);
        return new PlaylistEntryRow(entry.Id, entry.TrackId, index, entry.AddedAt);
    }

    public Task<PlaylistEntryChange> AddEntriesAsync(
        Guid userId,
        Guid playlistId,
        IReadOnlyList<Guid> trackIds,
        int? position,
        int maxEntries,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            userId,
            playlistId,
            now,
            async token =>
            {
                int count = await dbContext.PlaylistEntries.CountAsync(
                    entry => entry.PlaylistId == playlistId,
                    token);
                int start = position ?? count;
                if (start < 0 || start > count)
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.InvalidPosition, count);
                }

                if (count + trackIds.Count > maxEntries)
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.Full, maxEntries);
                }

                if (!await TracksExistAsync(trackIds, token))
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.TrackNotFound);
                }

                int added = trackIds.Count;
                await dbContext.PlaylistEntries
                    .Where(entry => entry.PlaylistId == playlistId && entry.Position >= start)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(entry => entry.Position, entry => entry.Position + added),
                        token);

                List<PlaylistEntry> entries = CreateEntries(playlistId, trackIds, start, now);
                dbContext.PlaylistEntries.AddRange(entries);
                await dbContext.SaveChangesAsync(token);
                return Done(entries);
            },
            cancellationToken);
    }

    public Task<PlaylistEntryChange> MoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        int position,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            userId,
            playlistId,
            now,
            async token =>
            {
                PlaylistEntry? entry = await FindInPlaylistAsync(playlistId, entryId, token);
                if (entry is null)
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.EntryNotFound);
                }

                int count = await dbContext.PlaylistEntries.CountAsync(
                    candidate => candidate.PlaylistId == playlistId,
                    token);
                if (position < 0 || position >= count)
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.InvalidPosition, count - 1);
                }

                int from = entry.Position;
                if (position < from)
                {
                    await dbContext.PlaylistEntries
                        .Where(candidate => candidate.PlaylistId == playlistId
                            && candidate.Position >= position
                            && candidate.Position < from)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(
                                candidate => candidate.Position,
                                candidate => candidate.Position + 1),
                            token);
                }
                else if (position > from)
                {
                    await dbContext.PlaylistEntries
                        .Where(candidate => candidate.PlaylistId == playlistId
                            && candidate.Position > from
                            && candidate.Position <= position)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(
                                candidate => candidate.Position,
                                candidate => candidate.Position - 1),
                            token);
                }

                await dbContext.PlaylistEntries
                    .Where(candidate => candidate.Id == entryId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.Position, position), token);
                return new PlaylistEntryChange(
                    PlaylistEntryChangeStatus.Done,
                    [new PlaylistEntryRow(entry.Id, entry.TrackId, position, entry.AddedAt)]);
            },
            cancellationToken);
    }

    public Task<PlaylistEntryChange> RemoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            userId,
            playlistId,
            now,
            async token =>
            {
                PlaylistEntry? entry = await FindInPlaylistAsync(playlistId, entryId, token);
                if (entry is null)
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.EntryNotFound);
                }

                await dbContext.PlaylistEntries
                    .Where(candidate => candidate.Id == entryId)
                    .ExecuteDeleteAsync(token);
                await dbContext.PlaylistEntries
                    .Where(candidate => candidate.PlaylistId == playlistId && candidate.Position > entry.Position)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            candidate => candidate.Position,
                            candidate => candidate.Position - 1),
                        token);
                return new PlaylistEntryChange(
                    PlaylistEntryChangeStatus.Done,
                    [new PlaylistEntryRow(entry.Id, entry.TrackId, entry.Position, entry.AddedAt)]);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<PlaylistCoverCandidateRow>> ListCoverCandidatesAsync(
        IReadOnlyCollection<Guid> playlistIds,
        int entriesPerPlaylist,
        CancellationToken cancellationToken = default)
    {
        if (playlistIds.Count == 0 || entriesPerPlaylist <= 0)
        {
            return [];
        }

        Guid[] ids = [.. playlistIds.Distinct()];
        return await dbContext.Playlists.AsNoTracking()
            .Where(playlist => ids.Contains(playlist.Id))
            .SelectMany(playlist => playlist.Entries
                .OrderBy(entry => entry.Position)
                .ThenBy(entry => entry.Id)
                .Take(entriesPerPlaylist))
            .Select(entry => new PlaylistCoverCandidateRow(
                entry.PlaylistId,
                entry.Position,
                entry.TrackId,
                entry.Track!.AlbumId,
                entry.Track.Covers.Any()
                    || (entry.Track.AlbumId != null
                        && dbContext.Covers.Any(cover =>
                            cover.AlbumId == entry.Track.AlbumId
                            || (cover.Track != null && cover.Track.AlbumId == entry.Track.AlbumId)))))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> ListExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new HashSet<Guid>();
        }

        Guid[] distinct = [.. ids.Distinct()];
        List<Guid> existing = await dbContext.Playlists.AsNoTracking()
            .Where(playlist => distinct.Contains(playlist.Id))
            .Select(playlist => playlist.Id)
            .ToListAsync(cancellationToken);
        return existing.ToHashSet();
    }

    private IQueryable<PlaylistRow> Project(IQueryable<Playlist> playlists)
    {
        return playlists.Select(playlist => new PlaylistRow(
            playlist.Id,
            playlist.Name,
            playlist.Entries.Count,
            playlist.Entries.Sum(entry => entry.Track!.Duration.HasValue
                ? entry.Track.Duration.Value.TotalMilliseconds
                : 0),
            playlist.CoverUpdatedAt,
            playlist.CreatedAt,
            playlist.UpdatedAt));
    }

    private IQueryable<PlaylistEntry> Ordered(Guid playlistId)
    {
        return dbContext.PlaylistEntries.AsNoTracking()
            .Where(entry => entry.PlaylistId == playlistId)
            .OrderBy(entry => entry.Position)
            .ThenBy(entry => entry.Id);
    }

    private Task<bool> ExistsAsync(Guid userId, Guid playlistId, CancellationToken cancellationToken)
    {
        return dbContext.Playlists.AnyAsync(
            playlist => playlist.Id == playlistId && playlist.UserId == userId,
            cancellationToken);
    }

    /// <summary>
    /// The index of an entry in the order of its playlist (position, then id), also while gaps are open.
    /// </summary>
    private async Task<int> IndexOfAsync(PlaylistEntry entry, CancellationToken cancellationToken)
    {
        int before = await dbContext.PlaylistEntries.CountAsync(
            candidate => candidate.PlaylistId == entry.PlaylistId && candidate.Position < entry.Position,
            cancellationToken);
        List<Guid> samePosition = await Ordered(entry.PlaylistId)
            .Where(candidate => candidate.Position == entry.Position)
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken);
        return before + Math.Max(0, samePosition.IndexOf(entry.Id));
    }

    private Task<PlaylistEntry?> FindInPlaylistAsync(
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        return dbContext.PlaylistEntries.AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == entryId && entry.PlaylistId == playlistId, cancellationToken);
    }

    private async Task<bool> TracksExistAsync(IReadOnlyList<Guid> trackIds, CancellationToken cancellationToken)
    {
        if (trackIds.Count == 0)
        {
            return true;
        }

        Guid[] distinct = [.. trackIds.Distinct()];
        int found = await dbContext.Tracks.CountAsync(track => distinct.Contains(track.Id), cancellationToken);
        return found == distinct.Length;
    }

    private static List<PlaylistEntry> CreateEntries(
        Guid playlistId,
        IReadOnlyList<Guid> trackIds,
        int start,
        DateTimeOffset now)
    {
        return [.. trackIds.Select((trackId, index) => new PlaylistEntry
        {
            Id = Guid.CreateVersion7(now),
            PlaylistId = playlistId,
            TrackId = trackId,
            Position = start + index,
            AddedAt = now,
        })];
    }

    private PlaylistEntryChange Done(IEnumerable<PlaylistEntry> entries)
    {
        List<PlaylistEntryRow> rows = [.. entries.Select(entry =>
            new PlaylistEntryRow(entry.Id, entry.TrackId, entry.Position, entry.AddedAt))];
        dbContext.ChangeTracker.Clear();
        return new PlaylistEntryChange(PlaylistEntryChangeStatus.Done, rows);
    }

    /// <summary>
    /// Runs a change of the entries in a transaction: the update of the playlist's time of change locks its row (other
    /// changes of the playlist wait), and the gaps left by removed tracks are closed first, so positions are indexes.
    /// Only a change that is done is committed.
    /// </summary>
    private Task<PlaylistEntryChange> ChangeAsync(
        Guid userId,
        Guid playlistId,
        DateTimeOffset now,
        Func<CancellationToken, Task<PlaylistEntryChange>> change,
        CancellationToken cancellationToken)
    {
        return InTransactionAsync(
            async token =>
            {
                int locked = await dbContext.Playlists
                    .Where(playlist => playlist.Id == playlistId && playlist.UserId == userId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(playlist => playlist.UpdatedAt, now), token);
                if (locked == 0)
                {
                    return PlaylistEntryChange.Of(PlaylistEntryChangeStatus.PlaylistNotFound);
                }

                await CloseGapsAsync(playlistId, token);
                return await change(token);
            },
            cancellationToken);
    }

    private Task CloseGapsAsync(Guid playlistId, CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlAsync(
            $"""
            UPDATE playlist_entries AS entry
            SET position = ranked.new_position
            FROM (
                SELECT id, (row_number() OVER (ORDER BY position, id) - 1)::integer AS new_position
                FROM playlist_entries
                WHERE playlist_id = {playlistId}
            ) AS ranked
            WHERE entry.id = ranked.id AND entry.position <> ranked.new_position
            """,
            cancellationToken);
    }

    /// <summary>
    /// Runs the work in a transaction that is committed only if the work is done; the execution strategy may repeat
    /// the whole work after a transient error.
    /// </summary>
    private Task<PlaylistEntryChange> InTransactionAsync(
        Func<CancellationToken, Task<PlaylistEntryChange>> work,
        CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(
            async token =>
            {
                dbContext.ChangeTracker.Clear();
                await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(token);
                PlaylistEntryChange result = await work(token);
                if (result.Status == PlaylistEntryChangeStatus.Done)
                {
                    await transaction.CommitAsync(token);
                }

                return result;
            },
            cancellationToken);
    }
}
