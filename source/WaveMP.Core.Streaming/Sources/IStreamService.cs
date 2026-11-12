using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// Finds the files a track plays and decides how a client plays them.
/// </summary>
public interface IStreamService
{
    /// <summary>
    /// The primary audio file of the track, checked on the disk: its size and time of the last change are read from
    /// the file system, not from the last scan.
    /// </summary>
    Task<StreamSourceResult> GetAudioSourceAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The file whose sound the track plays: its audio file (see <see cref="GetAudioSourceAsync"/>) or, if it has
    /// none, its video. <see cref="StreamSourceStatus.NoAudio"/> only for tracks without both.
    /// </summary>
    Task<StreamSourceResult> GetSoundSourceAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The file whose sound the track plays (see <see cref="GetSoundSourceAsync"/>) and whether a client that
    /// supports the <paramref name="supported"/> formats plays it directly or needs it transcoded. The sound of a video
    /// is always transcoded.
    /// </summary>
    Task<PlaybackResult> DecideAsync(
        Guid trackId,
        IReadOnlySet<AudioFormat> supported,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The primary video of the track, checked on the disk like the audio file.
    /// </summary>
    Task<VideoSourceResult> GetVideoSourceAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The video of the track (see <see cref="GetVideoSourceAsync"/>) and whether a client with the
    /// <paramref name="capabilities"/> plays it directly or needs it transcoded.
    /// </summary>
    Task<VideoPlaybackResult> DecideVideoAsync(
        Guid trackId,
        VideoCapabilities capabilities,
        CancellationToken cancellationToken = default);
}
