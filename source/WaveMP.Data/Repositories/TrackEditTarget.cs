using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// What the metadata editor writes for a track.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="DirectoryPath">Folder of the track relative to the media folder.</param>
/// <param name="AudioFile">The primary audio file of the track (not tracked); <c>null</c> for tracks with a video
/// only.</param>
public sealed record TrackEditTarget(Guid TrackId, string DirectoryPath, MediaFile? AudioFile);
