using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The audio file of a track, or why there is none.
/// </summary>
/// <param name="Status">Whether the file was found.</param>
/// <param name="Source">The file; set if <paramref name="Status"/> is <see cref="StreamSourceStatus.Found"/>.</param>
public sealed record StreamSourceResult(StreamSourceStatus Status, StreamSource? Source)
{
    [MemberNotNullWhen(true, nameof(Source))]
    public bool IsFound => Status == StreamSourceStatus.Found && Source is not null;

    public static StreamSourceResult Of(StreamSourceStatus status)
    {
        return new StreamSourceResult(status, null);
    }

    public static StreamSourceResult Found(StreamSource source)
    {
        return new StreamSourceResult(StreamSourceStatus.Found, source);
    }
}
