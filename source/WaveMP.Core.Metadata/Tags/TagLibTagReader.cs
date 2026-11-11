using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Reads tags with TagLib#. Media properties are not read (ffprobe provides them) and pictures are read lazily, so
/// only the tags are loaded. The file is closed before the method returns.
/// </summary>
internal sealed class TagLibTagReader : ITagReader
{
    public TagReadResult Read(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path, TagLib.ReadStyle.None | TagLib.ReadStyle.PictureLazy);
            TagLib.Tag tag = file.Tag;
            return new TagReadResult(TagReadStatus.Read, ToTags(tag), ToPicture(tag.Pictures));
        }
        catch (TagLib.UnsupportedFormatException)
        {
            return TagReadResult.Failed(TagReadStatus.Unsupported);
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
            return TagReadResult.Failed(TagReadStatus.Unreadable);
        }
    }

    private static MediaTags ToTags(TagLib.Tag tag)
    {
        return new MediaTags
        {
            Title = MetadataText.Normalize(tag.Title),
            TitleSort = MetadataText.Normalize(tag.TitleSort),
            Artist = MetadataText.Normalize(tag.FirstPerformer),
            Artists = MetadataText.Normalize(tag.JoinedPerformers),
            ArtistSort = MetadataText.Normalize(tag.FirstPerformerSort),
            AlbumArtist = MetadataText.Normalize(tag.FirstAlbumArtist),
            AlbumArtistSort = MetadataText.Normalize(tag.FirstAlbumArtistSort),
            Album = MetadataText.Normalize(tag.Album),
            AlbumSort = MetadataText.Normalize(tag.AlbumSort),
            TrackNumber = Positive(tag.Track),
            TrackCount = Positive(tag.TrackCount),
            DiscNumber = Positive(tag.Disc),
            DiscCount = Positive(tag.DiscCount),
            Year = Positive(tag.Year),
            Genre = MetadataText.Normalize(tag.FirstGenre),
            Genres = MetadataText.Normalize(tag.JoinedGenres),
            Composer = MetadataText.Normalize(tag.FirstComposer),
            ReplayGainTrackGain = Finite(tag.ReplayGainTrackGain),
            ReplayGainTrackPeak = Finite(tag.ReplayGainTrackPeak),
            ReplayGainAlbumGain = Finite(tag.ReplayGainAlbumGain),
            ReplayGainAlbumPeak = Finite(tag.ReplayGainAlbumPeak),
            MusicBrainzTrackId = MetadataText.Normalize(tag.MusicBrainzTrackId),
            MusicBrainzReleaseId = MetadataText.Normalize(tag.MusicBrainzReleaseId),
        };
    }

    private static EmbeddedPicture? ToPicture(TagLib.IPicture[]? pictures)
    {
        if (pictures is not { Length: > 0 })
        {
            return null;
        }

        TagLib.IPicture picture = pictures.FirstOrDefault(item => item.Type == TagLib.PictureType.FrontCover)
            ?? pictures[0];
        return new EmbeddedPicture(
            MetadataText.Normalize(picture.MimeType),
            picture.Type == TagLib.PictureType.FrontCover);
    }

    private static int? Positive(uint value)
    {
        return value is > 0 and <= int.MaxValue ? (int)value : null;
    }

    private static double? Finite(double value)
    {
        return double.IsFinite(value) ? value : null;
    }
}
