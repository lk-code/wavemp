namespace WaveMP.Core.Library.Covers;

/// <summary>
/// A cover ready to be delivered. The receiver disposes <paramref name="Content"/>.
/// </summary>
/// <param name="Content">The picture as it is stored, unscaled.</param>
/// <param name="ContentType">Type of the picture, one of <see cref="ImageFormats.ContentTypes"/>.</param>
/// <param name="CoverId">Id of the cover; with <paramref name="SourceModifiedAt"/> it identifies the version of the
/// picture, e.g. for caching.</param>
/// <param name="SourceModifiedAt">Time of the last change of the file the picture comes from.</param>
public sealed record CoverImage(Stream Content, string ContentType, Guid CoverId, DateTimeOffset SourceModifiedAt);
