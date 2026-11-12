using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The video of a track, or why there is none.
/// </summary>
/// <param name="Status">Whether the file was found.</param>
/// <param name="Source">The file; set if <paramref name="Status"/> is <see cref="StreamSourceStatus.Found"/>.</param>
public sealed record VideoSourceResult(StreamSourceStatus Status, VideoSource? Source)
{
    [MemberNotNullWhen(true, nameof(Source))]
    public bool IsFound => Status == StreamSourceStatus.Found && Source is not null;

    public static VideoSourceResult Of(StreamSourceStatus status)
    {
        return new VideoSourceResult(status, null);
    }

    public static VideoSourceResult Found(VideoSource source)
    {
        return new VideoSourceResult(StreamSourceStatus.Found, source);
    }
}
