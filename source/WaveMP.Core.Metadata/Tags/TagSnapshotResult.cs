using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// The raw tag values of a file and how reading them ended.
/// </summary>
public sealed record TagSnapshotResult(TagReadStatus Status, TagSnapshot? Snapshot)
{
    [MemberNotNullWhen(true, nameof(Snapshot))]
    public bool IsRead => Status == TagReadStatus.Read && Snapshot is not null;

    public static TagSnapshotResult Failed(TagReadStatus status)
    {
        return new TagSnapshotResult(status, null);
    }
}
