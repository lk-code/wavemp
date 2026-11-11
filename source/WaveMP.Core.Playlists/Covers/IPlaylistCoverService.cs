using WaveMP.Core.Caching.Covers;

namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// The own covers of the playlists of a user.
/// </summary>
public interface IPlaylistCoverService
{
    /// <summary>
    /// Stores an uploaded picture (JPEG, PNG or WebP, recognized by its content) as own cover of the playlist,
    /// cropped to a square around its center.
    /// </summary>
    Task<PlaylistCoverResult> SetAsync(
        Guid userId,
        Guid playlistId,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the own cover; the playlist shows covers of its tracks again. Returns <c>false</c> if the playlist does
    /// not exist; a playlist without an own cover is fine.
    /// </summary>
    Task<bool> RemoveAsync(Guid userId, Guid playlistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The own cover as WebP in the size (rounded up to 96, 300 or 1000; the largest without a size).
    /// </summary>
    Task<CoverDeliveryResult> GetAsync(
        Guid userId,
        Guid playlistId,
        int? size,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored covers of playlists that no longer exist, e.g. after a user was deleted.
    /// </summary>
    Task RemoveOrphansAsync(CancellationToken cancellationToken = default);
}
