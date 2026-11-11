namespace WaveMP.Core.Library.Covers;

/// <summary>
/// Delivers the covers of tracks and albums as stored (unscaled), following the cover rule of the library settings.
/// </summary>
public interface ICoverService
{
    /// <summary>
    /// The cover of a track: by default the cover of its album, otherwise its own, otherwise the cover of another
    /// track of its album. Covers whose file is missing or unreadable are skipped.
    /// </summary>
    Task<CoverResult> GetTrackCoverAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The cover of an album: by default the picture in its folder, otherwise the cover of one of its tracks.
    /// </summary>
    Task<CoverResult> GetAlbumCoverAsync(Guid albumId, CancellationToken cancellationToken = default);
}
