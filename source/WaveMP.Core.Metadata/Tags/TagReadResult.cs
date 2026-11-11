namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// The tags of a file and how reading them ended.
/// </summary>
public sealed record TagReadResult(TagReadStatus Status, MediaTags Tags, EmbeddedPicture? Picture)
{
    public static TagReadResult Failed(TagReadStatus status)
    {
        return new TagReadResult(status, MediaTags.Empty, null);
    }
}
