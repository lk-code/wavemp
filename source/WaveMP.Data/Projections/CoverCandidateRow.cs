using WaveMP.Data.Entities;

namespace WaveMP.Data.Projections;

/// <summary>
/// A cover that may be shown for a track or an album, with the file it comes from.
/// </summary>
/// <param name="CoverId">Id of the cover.</param>
/// <param name="MediaFileId">Id of the file the picture comes from: the picture file, or the audio or video file it is
/// embedded in. With <paramref name="Source"/>, <paramref name="Size"/> and <paramref name="ModifiedAt"/> it identifies
/// the version of the picture, e.g. for the cache.</param>
/// <param name="AlbumId">Id of the album for covers of an album (pictures in its folder).</param>
/// <param name="TrackId">Id of the track for covers of a track.</param>
/// <param name="TrackAlbumId">Id of the album of the track for covers of a track; <c>null</c> for covers of an album
/// and for tracks without album.</param>
/// <param name="DiscNumber">Disc number of the track of the cover.</param>
/// <param name="TrackNumber">Track number of the track of the cover.</param>
/// <param name="TrackTitle">Title of the track of the cover.</param>
/// <param name="IsPrimary">The cover chosen automatically for its album or track.</param>
/// <param name="Source">A picture file or a picture embedded in an audio or video file.</param>
/// <param name="RelativePath">Path of the file relative to the media folder.</param>
/// <param name="MimeType">Type of the picture, if known.</param>
/// <param name="Size">Size of the file in bytes.</param>
/// <param name="ModifiedAt">Time of the last change of the file.</param>
public sealed record CoverCandidateRow(
    Guid CoverId,
    Guid MediaFileId,
    Guid? AlbumId,
    Guid? TrackId,
    Guid? TrackAlbumId,
    int? DiscNumber,
    int? TrackNumber,
    string? TrackTitle,
    bool IsPrimary,
    CoverSource Source,
    string RelativePath,
    string? MimeType,
    long Size,
    DateTimeOffset ModifiedAt);
