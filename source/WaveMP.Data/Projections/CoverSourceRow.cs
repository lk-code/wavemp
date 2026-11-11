using WaveMP.Data.Entities;

namespace WaveMP.Data.Projections;

/// <summary>
/// A version of a picture that covers come from: the file and where in it the picture is, with the size and time of the
/// last change of the file.
/// </summary>
/// <param name="MediaFileId">Id of the picture file, or of the audio or video file the picture is embedded in.</param>
/// <param name="Source">A picture file or a picture embedded in an audio or video file.</param>
/// <param name="Size">Size of the file in bytes.</param>
/// <param name="ModifiedAt">Time of the last change of the file.</param>
public sealed record CoverSourceRow(Guid MediaFileId, CoverSource Source, long Size, DateTimeOffset ModifiedAt);
