using System.Diagnostics.CodeAnalysis;
using WaveMP.Core.Streaming.Decisions;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The audio file of a track with the decision how a client plays it, or why there is no file.
/// </summary>
/// <param name="Status">Whether the file was found.</param>
/// <param name="Source">The file; set if found.</param>
/// <param name="Decision">Direct play or transcoding; set if found.</param>
public sealed record PlaybackResult(StreamSourceStatus Status, StreamSource? Source, PlaybackDecision? Decision)
{
    [MemberNotNullWhen(true, nameof(Source), nameof(Decision))]
    public bool IsFound => Status == StreamSourceStatus.Found && Source is not null && Decision is not null;
}
