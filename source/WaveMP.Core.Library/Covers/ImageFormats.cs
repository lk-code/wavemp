namespace WaveMP.Core.Library.Covers;

/// <summary>
/// The picture formats that covers are delivered in. The format is recognized by the content of the picture (its
/// signature), never by the file extension or the type stored in the tags; anything else, including SVG, is not
/// delivered.
/// </summary>
public static class ImageFormats
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string WebP = "image/webp";
    public const string Gif = "image/gif";
    public const string Bmp = "image/bmp";

    /// <summary>
    /// Number of bytes at the start of a picture that <see cref="Detect"/> needs.
    /// </summary>
    public const int HeaderLength = 12;

    /// <summary>
    /// All content types of covers.
    /// </summary>
    public static IReadOnlyList<string> ContentTypes { get; } = [Jpeg, Png, WebP, Gif, Bmp];

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// The content type of a picture from its first bytes; <c>null</c> for other formats.
    /// </summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header is [0xFF, 0xD8, 0xFF, ..])
        {
            return Jpeg;
        }

        if (header.StartsWith(PngSignature))
        {
            return Png;
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return Gif;
        }

        if (header.Length >= HeaderLength && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return WebP;
        }

        return header.StartsWith("BM"u8) ? Bmp : null;
    }
}
