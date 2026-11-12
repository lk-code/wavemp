using WaveMP.Data.Entities;

namespace WaveMP.Data.Projections;

/// <summary>
/// A track with the file of one kind it plays.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="File">The primary file of the kind; <c>null</c> if the track has no file of this kind.</param>
/// <param name="VideoOffsetMilliseconds">
/// Offset of the video against the audio in milliseconds (see <see cref="Track.VideoOffsetMilliseconds"/>).
/// </param>
public sealed record TrackFileRow(Guid TrackId, MediaFile? File, int VideoOffsetMilliseconds = 0);
