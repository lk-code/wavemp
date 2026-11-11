using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// The playlists of the users and their entries. Every access names the user; playlists of other users are not found.
/// Changes of the entries lock the playlist, so parallel changes of the same playlist run one after the other.
/// Nothing is tracked.
/// </summary>
public interface IPlaylistRepository
{
    Task<Page<PlaylistRow>> ListAsync(PlaylistListQuery query, CancellationToken cancellationToken = default);

    Task<PlaylistRow?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a playlist with its first entries in the given order.
    /// </summary>
    /// <returns><see cref="PlaylistEntryChangeStatus.Done"/>, or
    /// <see cref="PlaylistEntryChangeStatus.TrackNotFound"/> if a track does not exist (nothing is created).</returns>
    Task<PlaylistEntryChange> CreateAsync(
        Playlist playlist,
        IReadOnlyList<Guid> trackIds,
        CancellationToken cancellationToken = default);

    Task<bool> RenameAsync(
        Guid userId,
        Guid id,
        string name,
        string normalizedName,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the own cover was uploaded (<paramref name="coverUpdatedAt"/>) or removed (<c>null</c>).
    /// </summary>
    Task<bool> SetCoverAsync(
        Guid userId,
        Guid id,
        DateTimeOffset? coverUpdatedAt,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of the entries of a playlist in its order, or <c>null</c> if the playlist does not exist.
    /// </summary>
    Task<Page<PlaylistEntryRow>?> ListEntriesAsync(
        PlaylistEntryListQuery query,
        CancellationToken cancellationToken = default);

    Task<PlaylistEntryRow?> FindEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the tracks in the given order at <paramref name="position"/> (at the end if <c>null</c>); the entries from
    /// there on move back.
    /// </summary>
    Task<PlaylistEntryChange> AddEntriesAsync(
        Guid userId,
        Guid playlistId,
        IReadOnlyList<Guid> trackIds,
        int? position,
        int maxEntries,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an entry to <paramref name="position"/>; the entries in between move by one.
    /// </summary>
    Task<PlaylistEntryChange> MoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        int position,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<PlaylistEntryChange> RemoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The first <paramref name="entriesPerPlaylist"/> entries of each playlist in their order, with whether their
    /// track has a cover.
    /// </summary>
    Task<IReadOnlyList<PlaylistCoverCandidateRow>> ListCoverCandidatesAsync(
        IReadOnlyCollection<Guid> playlistIds,
        int entriesPerPlaylist,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The ids among <paramref name="ids"/> whose playlist exists, of any user.
    /// </summary>
    Task<IReadOnlySet<Guid>> ListExistingIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
}
