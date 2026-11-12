using System.Globalization;
using WaveMP.Core.Metadata.Codecs;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Probes;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Metadata.Texts;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Readers;

/// <summary>
/// Reads tags with TagLib# first, then checks whether audio files are writable and finally runs ffprobe; on Windows
/// a file still opened by TagLib# could not be checked. Tag values that TagLib# does not find come from ffprobe.
/// </summary>
internal sealed class MediaMetadataReader(ITagReader tagReader, IMediaProber prober, IFileWriteAccess writeAccess)
    : IMediaMetadataReader
{
    public async Task<MediaFileMetadata> ReadAsync(
        string path,
        MediaFileKind kind,
        CancellationToken cancellationToken = default)
    {
        TagReadResult tags = kind == MediaFileKind.Image
            ? TagReadResult.Failed(TagReadStatus.Unsupported)
            : tagReader.Read(path);
        bool isWritable = kind == MediaFileKind.Audio
            && tags.Status == TagReadStatus.Read
            && writeAccess.IsWritable(path);
        MediaProbeResult? probe = await prober.ProbeAsync(path, cancellationToken);

        MediaFileIssue? issue = (kind, probe, tags.Status) switch
        {
            (MediaFileKind.Image, null, _) => MediaFileIssue.Unreadable,
            (_, null, TagReadStatus.Unreadable) => MediaFileIssue.Unreadable,
            (_, null, _) => MediaFileIssue.ProbeFailed,
            (_, _, TagReadStatus.Unreadable) => MediaFileIssue.TagsUnreadable,
            _ => null,
        };

        MediaTags mediaTags = probe is null ? tags.Tags : Complete(tags.Tags, FromProbeTags(probe.Tags));
        PictureStreamInfo? attached = kind == MediaFileKind.Image ? null : probe?.AttachedPicture;

        MediaFileMetadata metadata = new()
        {
            Tags = mediaTags,
            Format = Truncate(probe?.FormatName),
            IsWritable = isWritable,
            Issue = issue,
            HasEmbeddedPicture = tags.Picture is not null || attached is not null,
            EmbeddedPictureWidth = attached?.Width,
            EmbeddedPictureHeight = attached?.Height,
            EmbeddedPictureMimeType = tags.Picture?.MimeType ?? MimeTypeOfCodec(attached?.Codec),
        };

        if (probe is null)
        {
            return metadata;
        }

        return kind switch
        {
            MediaFileKind.Audio => metadata with
            {
                Codec = Truncate(probe.Audio?.Codec),
                Bitrate = ToInt(probe.Audio?.Bitrate ?? probe.Bitrate),
                SampleRate = probe.Audio?.SampleRate,
                BitDepth = probe.Audio?.BitDepth,
                Channels = probe.Audio?.Channels,
                IsLossless = LosslessCodecs.IsLossless(probe.Audio?.Codec),
                Duration = probe.Duration,
            },
            MediaFileKind.Video => metadata with
            {
                Codec = Truncate(probe.Video?.Codec),
                AudioCodec = Truncate(probe.Audio?.Codec),
                PixelFormat = Truncate(probe.Video?.PixelFormat),
                Bitrate = ToInt(probe.Video?.Bitrate ?? probe.Bitrate),
                SampleRate = probe.Audio?.SampleRate,
                Channels = probe.Audio?.Channels,
                Width = probe.Video?.Width,
                Height = probe.Video?.Height,
                Duration = probe.Duration,
            },
            _ => metadata with
            {
                Codec = Truncate(probe.Video?.Codec),
                Width = probe.Video?.Width,
                Height = probe.Video?.Height,
            },
        };
    }

    /// <summary>
    /// Tags as ffprobe reports them, for formats without TagLib# support.
    /// </summary>
    internal static MediaTags FromProbeTags(IReadOnlyDictionary<string, string> tags)
    {
        string? Get(params string[] keys)
        {
            return keys
                .Select(key => tags.TryGetValue(key, out string? value) ? MetadataText.Normalize(value) : null)
                .FirstOrDefault(value => value is not null);
        }

        (int? trackNumber, int? trackCount) = ParseNumberAndCount(Get("track", "tracknumber"));
        (int? discNumber, int? discCount) = ParseNumberAndCount(Get("disc", "discnumber"));
        string? artist = Get("artist", "performer");
        string? genre = Get("genre");

        return new MediaTags
        {
            Title = Get("title"),
            Artist = artist,
            Artists = artist,
            AlbumArtist = Get("album_artist", "albumartist", "album artist"),
            Album = Get("album"),
            TrackNumber = trackNumber,
            TrackCount = trackCount ?? ParseNumber(Get("tracktotal", "totaltracks")),
            DiscNumber = discNumber,
            DiscCount = discCount ?? ParseNumber(Get("disctotal", "totaldiscs")),
            Year = ParseYear(Get("date", "year", "originaldate")),
            Genre = genre,
            Genres = genre,
            Composer = Get("composer"),
        };
    }

    /// <summary>
    /// The tags of TagLib# with the values it did not find taken from ffprobe, e.g. for formats TagLib# does not
    /// support or tags it does not map (such as the artist of Matroska files).
    /// </summary>
    internal static MediaTags Complete(MediaTags tags, MediaTags fallback)
    {
        return tags with
        {
            Title = tags.Title ?? fallback.Title,
            Artist = tags.Artist ?? fallback.Artist,
            Artists = tags.Artists ?? fallback.Artists,
            AlbumArtist = tags.AlbumArtist ?? fallback.AlbumArtist,
            Album = tags.Album ?? fallback.Album,
            TrackNumber = tags.TrackNumber ?? fallback.TrackNumber,
            TrackCount = tags.TrackCount ?? fallback.TrackCount,
            DiscNumber = tags.DiscNumber ?? fallback.DiscNumber,
            DiscCount = tags.DiscCount ?? fallback.DiscCount,
            Year = tags.Year ?? fallback.Year,
            Genre = tags.Genre ?? fallback.Genre,
            Genres = tags.Genres ?? fallback.Genres,
            Composer = tags.Composer ?? fallback.Composer,
        };
    }

    /// <summary>
    /// The year of a date such as <c>2001</c>, <c>2001-05-12</c> or <c>2001/05</c>.
    /// </summary>
    internal static int? ParseYear(string? date)
    {
        return date is { Length: >= 4 }
            && int.TryParse(date.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out int year)
            && year is >= 1000 and <= 9999
            && (date.Length == 4 || !char.IsAsciiDigit(date[4]))
                ? year
                : null;
    }

    private static (int? Number, int? Count) ParseNumberAndCount(string? value)
    {
        if (value is null)
        {
            return (null, null);
        }

        string[] parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        return (ParseNumber(parts[0]), parts.Length > 1 ? ParseNumber(parts[1]) : null);
    }

    private static int? ParseNumber(string? value)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number > 0
            ? number
            : null;
    }

    private static int? ToInt(long? value)
    {
        return value is > 0 and <= int.MaxValue ? (int)value.Value : null;
    }

    private static string? Truncate(string? value)
    {
        return value is { Length: > 64 } ? value[..64] : value;
    }

    private static string? MimeTypeOfCodec(string? codec)
    {
        return codec switch
        {
            "mjpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            _ => null,
        };
    }
}
