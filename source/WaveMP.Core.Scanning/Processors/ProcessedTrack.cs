using WaveMP.Core.Metadata.Resolutions;
using WaveMP.Core.Scanning.Groupings;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Processors;

/// <summary>
/// A track of a folder as the scan sees it: its files, the files it plays, its metadata, album and covers.
/// </summary>
/// <param name="Group">The files with the base name of the track.</param>
/// <param name="Metadata">Metadata from tags, folder and file name.</param>
/// <param name="Album">The album, or <c>null</c> for tracks without album.</param>
/// <param name="PrimaryAudio">The audio file of the highest quality.</param>
/// <param name="PrimaryVideo">The video file of the highest quality.</param>
/// <param name="Covers">The available covers, the one to show first.</param>
public sealed record ProcessedTrack(
    FileGroup Group,
    ResolvedTrackMetadata Metadata,
    AlbumAssignment? Album,
    MediaFile? PrimaryAudio,
    MediaFile? PrimaryVideo,
    IReadOnlyList<CoverCandidate> Covers)
{
    /// <summary>
    /// Duration of the primary audio file, otherwise of the primary video file.
    /// </summary>
    public TimeSpan? Duration => PrimaryAudio?.Duration ?? PrimaryVideo?.Duration;
}
