using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Covers;

/// <summary>
/// Reads the picture of a cover from the media folder, as stored (unscaled). Needs no database, so it can be used
/// across requests.
/// </summary>
public interface ICoverSourceReader
{
    /// <summary>
    /// The picture of the cover; <c>null</c> if its path leaves the media folder, its file is missing or cannot be
    /// read, or it is no picture of a known format. The receiver disposes the content.
    /// </summary>
    Task<CoverImage?> OpenAsync(CoverCandidateRow candidate, CancellationToken cancellationToken = default);
}
