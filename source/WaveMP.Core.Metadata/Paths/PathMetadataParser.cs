using System.Globalization;
using System.Text.RegularExpressions;
using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// Derives metadata from the folder structure and the file name, for files without (complete) tags:
/// <list type="bullet">
/// <item><description><c>Artist/Album/01 - Title</c>, also with disc folders such as <c>CD2</c>;</description></item>
/// <item><description><c>Artist - Album/02. Title</c> and <c>Artist/Title</c>;</description></item>
/// <item><description><c>Artist - Title</c> and <c>Artist - Album - Title</c> as file names;</description></item>
/// <item><description>a pattern shared by the names of a folder (<see cref="FileNamePatternDetector"/>).</description></item>
/// </list>
/// A leading number is only a track number in an album folder or with a leading zero, so <c>50 Cent - Song</c> stays
/// an artist.
/// </summary>
public static partial class PathMetadataParser
{
    private const int MinYear = 1900;
    private const int MaxYear = 2100;

    /// <param name="directorySegments">Folders from the media folder down to the file.</param>
    /// <param name="baseName">File name without extension.</param>
    /// <param name="pattern">Pattern shared by the names of the folder, if one was found.</param>
    public static PathMetadata Parse(
        IReadOnlyList<string> directorySegments,
        string baseName,
        FileNamePattern? pattern = null)
    {
        (string? folderArtist, string? folderAlbum, int? folderDisc, int? year) = ParseFolders(directorySegments);
        FileNameFields fields = pattern?.Match(baseName)
            ?? ParseFileName(baseName, inAlbumFolder: folderAlbum is not null);

        string? fileArtist = fields.Artist;
        if (fileArtist is not null && folderArtist is not null && fields.Album is null
            && MetadataText.ToKey(fileArtist) == MetadataText.ToKey(folderArtist))
        {
            fileArtist = folderArtist;
        }

        string cleanName = FileNameTokens.Clean(baseName);
        string title = fields.Title ?? (cleanName.Length > 0 ? cleanName : baseName);
        string? albumArtist = fileArtist is not null && folderArtist is not null
            && MetadataText.ToKey(fileArtist) != MetadataText.ToKey(folderArtist)
                ? folderArtist
                : null;

        return new PathMetadata(
            title,
            IsTitleFromPattern: title != cleanName,
            Artist: fileArtist ?? folderArtist,
            AlbumArtist: albumArtist,
            Album: fields.Album ?? folderAlbum,
            TrackNumber: fields.TrackNumber,
            DiscNumber: fields.DiscNumber ?? folderDisc,
            Year: year);
    }

    /// <summary>
    /// Reads a file name on its own, without a pattern of its folder.
    /// </summary>
    public static FileNameFields ParseFileName(string baseName, bool inAlbumFolder)
    {
        int? track = null;
        int? disc = null;
        string rest = baseName.Trim();

        if (FileNameTokens.SplitDiscAndTrack(rest) is { } discAndTrack
            && (inAlbumFolder || discAndTrack.Track.StartsWith('0')))
        {
            disc = FileNameTokens.ParseNumber(discAndTrack.Disc);
            track = FileNameTokens.ParseNumber(discAndTrack.Track);
            rest = discAndTrack.Remainder;
        }
        else
        {
            (string? number, string afterNumber) = FileNameTokens.SplitNumber(rest);
            if (number is not null && (inAlbumFolder || number.StartsWith('0')))
            {
                track = FileNameTokens.ParseNumber(number);
                rest = afterNumber;
            }
        }

        string[] texts = FileNameTokens.SplitTexts(rest);
        return texts.Length switch
        {
            0 => new FileNameFields(track, disc),
            1 => new FileNameFields(track, disc, Title: texts[0]),
            2 => new FileNameFields(track, disc, Artist: texts[0], Title: texts[1]),
            3 => new FileNameFields(track, disc, Artist: texts[0], Album: texts[1], Title: texts[2]),
            _ => new FileNameFields(track, disc, Artist: texts[0], Title: string.Join(" - ", texts[1..])),
        };
    }

    private static (string? Artist, string? Album, int? Disc, int? Year) ParseFolders(IReadOnlyList<string> segments)
    {
        List<string> folders = [.. segments.Select(MetadataText.Normalize).OfType<string>()];
        int? disc = null;
        if (folders.Count > 0 && DiscFolders.TryParse(folders[^1], out int discNumber))
        {
            disc = discNumber;
            folders.RemoveAt(folders.Count - 1);
        }

        string? artist = null;
        string? album = null;
        if (folders.Count >= 2)
        {
            artist = folders[^2];
            album = folders[^1];
        }
        else if (folders.Count == 1)
        {
            string[] parts = FileNameTokens.SplitTexts(folders[0]);
            if (parts.Length == 2)
            {
                (artist, album) = (parts[0], parts[1]);
            }
            else
            {
                artist = folders[0];
            }
        }

        (album, int? year) = SplitYear(album);
        return (artist, album, disc, year);
    }

    /// <summary>
    /// Takes a year out of an album name such as <c>2001 - Album</c>, <c>(2001) Album</c>, <c>[2001] Album</c> or
    /// <c>Album (2001)</c>.
    /// </summary>
    internal static (string? Album, int? Year) SplitYear(string? album)
    {
        if (album is null)
        {
            return (null, null);
        }

        foreach (Regex pattern in (Regex[])[YearFirstPattern(), BracketYearFirstPattern(), YearLastPattern()])
        {
            Match match = pattern.Match(album);
            if (match.Success
                && int.TryParse(match.Groups["year"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int year)
                && year is >= MinYear and <= MaxYear
                && MetadataText.Normalize(match.Groups["album"].Value) is { } name)
            {
                return (name, year);
            }
        }

        return (album, null);
    }

    [GeneratedRegex(@"^(?<year>\d{4})\s*[-–—]\s*(?<album>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex YearFirstPattern();

    [GeneratedRegex(@"^[\(\[](?<year>\d{4})[\)\]]\s*[-–—]?\s*(?<album>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex BracketYearFirstPattern();

    [GeneratedRegex(@"^(?<album>.+?)\s*[\(\[](?<year>\d{4})[\)\]]$", RegexOptions.CultureInvariant)]
    private static partial Regex YearLastPattern();
}
