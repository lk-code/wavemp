using System.Security.Cryptography;
using System.Text;
using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Resolutions;

/// <summary>
/// The identity of an album: its normalized album artist (or compilation) and title, hashed with SHA-256 to a short
/// key of fixed length for the unique index.
/// </summary>
public static class AlbumKeys
{
    public static string Create(string title, string? albumArtist, bool isCompilation)
    {
        string normalizedTitle = MetadataText.ToKey(title) ?? string.Empty;
        string owner = isCompilation
            ? "compilation"
            : MetadataText.ToKey(albumArtist) is { } artist
                ? "artist:" + artist
                : "unknown";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(owner + "|" + normalizedTitle));
        return Convert.ToHexStringLower(hash);
    }
}
