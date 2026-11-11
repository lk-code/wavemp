namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// A cover ready to be delivered. The receiver disposes <paramref name="Content"/>.
/// </summary>
/// <param name="Content">The scaled picture, or the picture as stored.</param>
/// <param name="ContentType">Type of the picture: <c>image/webp</c> for scaled pictures.</param>
/// <param name="ETag">Strong entity tag (without quotes) of this version and size of the picture.</param>
/// <param name="LastModified">Time of the last change of the file the picture comes from.</param>
public sealed record CoverDelivery(Stream Content, string ContentType, string ETag, DateTimeOffset LastModified);
