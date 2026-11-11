namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// A picture embedded in the tags of a file: the front cover, or the first picture if there is none.
/// </summary>
/// <param name="MimeType">Type of the picture, e.g. <c>image/jpeg</c>; <c>null</c> if unknown.</param>
/// <param name="IsFrontCover">Whether the tags mark it as the front cover.</param>
public sealed record EmbeddedPicture(string? MimeType, bool IsFrontCover);
