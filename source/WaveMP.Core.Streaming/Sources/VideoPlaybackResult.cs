using System.Diagnostics.CodeAnalysis;
using WaveMP.Core.Streaming.Decisions;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The video of a track with the decision how a client plays it, or why there is no video.
/// </summary>
/// <param name="Status">Whether the video was found.</param>
/// <param name="Source">The video; set if found.</param>
/// <param name="Decision">Direct play or transcoding; set if found.</param>
public sealed record VideoPlaybackResult(
    StreamSourceStatus Status,
    VideoSource? Source,
    VideoPlaybackDecision? Decision)
{
    [MemberNotNullWhen(true, nameof(Source), nameof(Decision))]
    public bool IsFound => Status == StreamSourceStatus.Found && Source is not null && Decision is not null;
}
