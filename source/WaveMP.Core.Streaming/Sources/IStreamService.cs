using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// Finds the audio file a track plays and decides how a client plays it.
/// </summary>
public interface IStreamService
{
    /// <summary>
    /// The primary audio file of the track, checked on the disk: its size and time of the last change are read from
    /// the file system, not from the last scan.
    /// </summary>
    Task<StreamSourceResult> GetAudioSourceAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The audio file of the track (see <see cref="GetAudioSourceAsync"/>) and whether a client that supports the
    /// <paramref name="supported"/> formats plays it directly or needs it transcoded.
    /// </summary>
    Task<PlaybackResult> DecideAsync(
        Guid trackId,
        IReadOnlySet<AudioFormat> supported,
        CancellationToken cancellationToken = default);
}
