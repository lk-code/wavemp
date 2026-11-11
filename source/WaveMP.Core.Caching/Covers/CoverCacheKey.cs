using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// Names and ETags of scaled covers. They belong to the version of the picture, not to an album or track: the prefix
/// <c>{file id}-{f|e}-{stamp}</c> names the file the picture comes from, whether it is a picture file (<c>f</c>) or
/// embedded (<c>e</c>), and a stamp of the size and the time of the last change of the file and of
/// <see cref="FormatVersion"/>. A changed file therefore gets new names and ETags at once, and albums and tracks that
/// show the same picture share its files.
/// </summary>
public static partial class CoverCacheKey
{
    /// <summary>
    /// Version of the conversion; increase it when the pictures change (e.g. another quality), so that older files are
    /// replaced.
    /// </summary>
    public const int FormatVersion = 1;

    private const int IdLength = 32;
    private const int ShardLength = 2;
    private const int StampBytes = 8;

    public static string PrefixOf(CoverCandidateRow candidate)
    {
        return PrefixOf(candidate.MediaFileId, candidate.Source, candidate.ModifiedAt, candidate.Size);
    }

    public static string PrefixOf(CoverSourceRow source)
    {
        return PrefixOf(source.MediaFileId, source.Source, source.ModifiedAt, source.Size);
    }

    public static string PrefixOf(Guid mediaFileId, CoverSource source, DateTimeOffset modifiedAt, long size)
    {
        string version = string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatVersion}:{modifiedAt.UtcTicks}:{size}");
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(version));
        string stamp = Convert.ToHexStringLower(hash, 0, StampBytes);
        return $"{mediaFileId:N}-{(source == CoverSource.File ? 'f' : 'e')}-{stamp}";
    }

    /// <summary>
    /// Name of the scaled picture in the area of the covers: in a folder of the last two characters of the file id
    /// (the random part of a UUIDv7), so no folder grows too large.
    /// </summary>
    public static string FileNameOf(string prefix, int size)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix.AsSpan(IdLength - ShardLength, ShardLength)}/{prefix}-{size}.webp");
    }

    /// <summary>
    /// The ETag (without quotes) of a scaled picture.
    /// </summary>
    public static string ETagOf(string prefix, int size)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{prefix}-{size}");
    }

    /// <summary>
    /// The ETag (without quotes) of the unscaled picture.
    /// </summary>
    public static string OriginalETagOf(string prefix)
    {
        return prefix + "-original";
    }

    /// <summary>
    /// The prefix of the name of a scaled picture; <c>false</c> for names that the cache did not create this way.
    /// </summary>
    public static bool TryReadSourcePrefix(string fileName, out string prefix)
    {
        prefix = string.Empty;
        Match match = FileNamePattern().Match(fileName);
        if (!match.Success || !fileName.AsSpan(0, ShardLength).SequenceEqual(
            match.Groups[1].ValueSpan.Slice(IdLength - ShardLength, ShardLength)))
        {
            return false;
        }

        prefix = match.Groups[1].Value;
        return true;
    }

    [GeneratedRegex(
        "^[0-9a-f]{2}/([0-9a-f]{32}-[ef]-[0-9a-f]{16})-[0-9]{1,4}\\.webp$",
        RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();
}
