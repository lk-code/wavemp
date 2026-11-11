using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Writes tags with TagLib#, in place. Only the changed fields are set; sort fields, other fields and embedded
/// pictures stay. Kinds of tags the file does not have are not added (TagLib# would add e.g. an ID3v1 tag to every
/// MP3 file), except the native tag of the format if the file has none. The file is closed before the method returns.
/// </summary>
internal sealed class TagLibTagWriter : ITagWriter
{
    public TagSnapshotResult ReadSnapshot(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path, TagLib.ReadStyle.None | TagLib.ReadStyle.PictureLazy);
            TagLib.Tag tag = file.Tag;
            return new TagSnapshotResult(TagReadStatus.Read, new TagSnapshot(
                tag.Title,
                Copy(tag.Performers),
                Copy(tag.AlbumArtists),
                tag.Album,
                Positive(tag.Year),
                Positive(tag.Track),
                Positive(tag.TrackCount),
                Positive(tag.Disc),
                Positive(tag.DiscCount),
                Copy(tag.Genres)));
        }
        catch (TagLib.UnsupportedFormatException)
        {
            return TagSnapshotResult.Failed(TagReadStatus.Unsupported);
        }
        catch (Exception exception) when (exception is TagLib.CorruptFileException
            or IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotImplementedException
            or IndexOutOfRangeException
            or OverflowException)
        {
            // TagLib# reports some damaged files with general exceptions.
            return TagSnapshotResult.Failed(TagReadStatus.Unreadable);
        }
    }

    public TagWriteStatus Write(string path, MediaFileKind kind, TagChanges changes)
    {
        if (kind != MediaFileKind.Audio)
        {
            return TagWriteStatus.NotAudio;
        }

        if (changes.IsEmpty)
        {
            return TagWriteStatus.Written;
        }

        try
        {
            // Pictures are read completely (no PictureLazy), so they are written back unchanged.
            using var file = TagLib.File.Create(path);
            PrepareTags(file);
            Apply(file.Tag, changes);
            file.Save();
            return TagWriteStatus.Written;
        }
        catch (Exception exception) when (exception is TagLib.UnsupportedFormatException
            or NotImplementedException
            or NotSupportedException)
        {
            return TagWriteStatus.Unsupported;
        }
        catch (Exception exception) when (exception is TagLib.CorruptFileException
            or ArgumentException
            or IndexOutOfRangeException
            or OverflowException)
        {
            return TagWriteStatus.Unreadable;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return TagWriteStatus.NotWritable;
        }
    }

    /// <summary>
    /// Adds the native tag of the format if it is missing and removes the tags TagLib# created while reading that are
    /// neither on disk nor native, so saving adds no other kinds of tags.
    /// </summary>
    private static void PrepareTags(TagLib.File file)
    {
        TagLib.TagTypes native = NativeTagOf(file);
        if (native != TagLib.TagTypes.None)
        {
            file.GetTag(native, true);
        }

        TagLib.TagTypes surplus = file.TagTypes & ~(file.TagTypesOnDisk | native);
        if (surplus != TagLib.TagTypes.None)
        {
            file.RemoveTags(surplus);
        }
    }

    private static TagLib.TagTypes NativeTagOf(TagLib.File file)
    {
        return file switch
        {
            TagLib.Mpeg.AudioFile or TagLib.Aiff.File or TagLib.Riff.File or TagLib.Dsf.File => TagLib.TagTypes.Id3v2,
            TagLib.Flac.File or TagLib.Ogg.File => TagLib.TagTypes.Xiph,
            TagLib.Mpeg4.File => TagLib.TagTypes.Apple,
            TagLib.Asf.File => TagLib.TagTypes.Asf,
            TagLib.Ape.File or TagLib.WavPack.File or TagLib.MusePack.File => TagLib.TagTypes.Ape,
            _ => TagLib.TagTypes.None,
        };
    }

    private static void Apply(TagLib.Tag tag, TagChanges changes)
    {
        if (changes.Title is { } title)
        {
            tag.Title = title.Value;
        }

        if (changes.Artist is { } artist)
        {
            tag.Performers = ToArray(artist.Value);
        }

        if (changes.AlbumArtist is { } albumArtist)
        {
            tag.AlbumArtists = ToArray(albumArtist.Value);
        }

        if (changes.Album is { } album)
        {
            tag.Album = album.Value;
        }

        if (changes.Year is { } year)
        {
            tag.Year = ToNumber(year.Value);
        }

        if (changes.TrackNumber is { } trackNumber)
        {
            tag.Track = ToNumber(trackNumber.Value);
            if (trackNumber.Value is null)
            {
                // A count without a number makes no sense. It is removed after the number: Xiph comments move the
                // count of "3/12" into a field of its own when the number changes, ID3v2 tags would keep "0/12".
                tag.TrackCount = 0;
            }
        }

        if (changes.DiscNumber is { } discNumber)
        {
            tag.Disc = ToNumber(discNumber.Value);
            if (discNumber.Value is null)
            {
                tag.DiscCount = 0;
            }
        }

        if (changes.Genre is { } genre)
        {
            tag.Genres = ToArray(genre.Value);
        }
    }

    private static string[] ToArray(string? value)
    {
        return value is null ? [] : [value];
    }

    private static uint ToNumber(int? value)
    {
        return value is > 0 ? (uint)value.Value : 0;
    }

    private static string[] Copy(string[]? values)
    {
        return values is null ? [] : [.. values];
    }

    private static int? Positive(uint value)
    {
        return value is > 0 and <= int.MaxValue ? (int)value : null;
    }
}
