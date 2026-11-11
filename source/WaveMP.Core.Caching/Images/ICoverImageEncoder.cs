namespace WaveMP.Core.Caching.Images;

/// <summary>
/// Scales a cover to WebP pictures of several sizes at once.
/// </summary>
public interface ICoverImageEncoder
{
    /// <summary>
    /// Reads the picture from <paramref name="source"/> and writes one WebP picture per target. The format comes from
    /// <paramref name="contentType"/> (one of <see cref="Library.Covers.ImageFormats.ContentTypes"/>), never from a
    /// file extension. Animated pictures give their first frame. Targets that were written are removed again if the
    /// conversion fails.
    /// </summary>
    Task<CoverEncodeStatus> EncodeAsync(
        Stream source,
        string contentType,
        IReadOnlyList<CoverEncodeTarget> targets,
        CancellationToken cancellationToken = default);
}
