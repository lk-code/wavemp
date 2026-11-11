namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// The content of a picture embedded in the tags of a file.
/// </summary>
/// <param name="Data">The bytes of the picture.</param>
/// <param name="MimeType">Type of the picture as stored in the tags, e.g. <c>image/jpeg</c>; <c>null</c> if unknown.
/// </param>
public sealed record EmbeddedPictureData(byte[] Data, string? MimeType);
