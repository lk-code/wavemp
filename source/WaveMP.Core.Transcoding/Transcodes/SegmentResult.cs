using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// A segment from the cache, or why there is none.
/// </summary>
/// <param name="Status">Whether the segment is ready.</param>
/// <param name="Content">The segment, opened for reading; set if ready. The receiver disposes it.</param>
public sealed record SegmentResult(SegmentStatus Status, Stream? Content)
{
    [MemberNotNullWhen(true, nameof(Content))]
    public bool IsReady => Status == SegmentStatus.Ready && Content is not null;

    public static SegmentResult Of(SegmentStatus status)
    {
        return new SegmentResult(status, null);
    }

    public static SegmentResult Ready(Stream content)
    {
        return new SegmentResult(SegmentStatus.Ready, content);
    }
}
