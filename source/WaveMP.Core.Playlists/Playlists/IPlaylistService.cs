using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Playlists;

/// <summary>
/// The playlists of a user and their entries. Every method names the user; playlists of other users are not found.
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Lists the playlists of the user of the query with their covers.
    /// </summary>
    Task<Page<PlaylistItem>> ListAsync(PlaylistListQuery query, CancellationToken cancellationToken = default);

    Task<PlaylistItem?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a playlist, optionally with its first tracks in the given order.
    /// </summary>
    Task<PlaylistResult> CreateAsync(
        Guid userId,
        string? name,
        IReadOnlyList<Guid> trackIds,
        CancellationToken cancellationToken = default);

    Task<PlaylistResult> RenameAsync(
        Guid userId,
        Guid id,
        string? name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a playlist with its entries and its own cover. Returns <c>false</c> if it does not exist.
    /// </summary>
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of the entries of a playlist in its order with their tracks, or <c>null</c> if the playlist does not
    /// exist.
    /// </summary>
    Task<Page<PlaylistEntryItem>?> ListEntriesAsync(
        Guid userId,
        Guid playlistId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// An entry of a playlist with its current position.
    /// </summary>
    Task<PlaylistEntriesResult> GetEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds tracks in the given order at a position (at the end if <c>null</c>).
    /// </summary>
    Task<PlaylistEntriesResult> AddEntriesAsync(
        Guid userId,
        Guid playlistId,
        IReadOnlyList<Guid> trackIds,
        int? position,
        CancellationToken cancellationToken = default);

    Task<PlaylistEntriesResult> MoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        int position,
        CancellationToken cancellationToken = default);

    Task<PlaylistStatus> RemoveEntryAsync(
        Guid userId,
        Guid playlistId,
        Guid entryId,
        CancellationToken cancellationToken = default);
}
