using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Whether a client plays a file directly or needs it transcoded.
/// </summary>
/// <param name="Method">Direct play or transcoding.</param>
/// <param name="Format">Format of the file; <c>null</c> if clients cannot play it directly.</param>
/// <param name="Reason">Why the file has to be transcoded; <c>null</c> for direct play.</param>
public sealed record PlaybackDecision(PlaybackMethod Method, AudioFormat? Format, TranscodeReason? Reason);
