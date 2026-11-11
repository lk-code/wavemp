using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Reads embedded pictures with TagLib#. The same picture is chosen as by <see cref="TagLibTagReader"/>; the file is
/// closed before the method returns.
/// </summary>
internal sealed class TagLibEmbeddedPictureReader : IEmbeddedPictureReader
{
    public EmbeddedPictureData? ReadPicture(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path, TagLib.ReadStyle.None | TagLib.ReadStyle.PictureLazy);
            TagLib.IPicture[]? pictures = file.Tag.Pictures;
            if (pictures is not { Length: > 0 })
            {
                return null;
            }

            TagLib.IPicture picture = pictures.FirstOrDefault(item => item.Type == TagLib.PictureType.FrontCover)
                ?? pictures[0];

            // Lazy pictures are read from the file here, so the data must be taken before the file is closed.
            TagLib.ByteVector? data = picture.Data;
            if (data is null || data.Count == 0 || data.Count > IEmbeddedPictureReader.MaxPictureSize)
            {
                return null;
            }

            return new EmbeddedPictureData(data.Data, MetadataText.Normalize(picture.MimeType));
        }
        catch (Exception exception) when (exception is TagLib.UnsupportedFormatException
            or TagLib.CorruptFileException
            or IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotImplementedException
            or IndexOutOfRangeException
            or OverflowException)
        {
            // TagLib# reports some damaged files with general exceptions; missing files are IOExceptions.
            return null;
        }
    }
}
