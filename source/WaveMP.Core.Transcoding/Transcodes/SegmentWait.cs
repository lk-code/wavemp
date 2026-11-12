namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// How waiting for a segment ended.
/// </summary>
internal enum SegmentWait
{
    Ready,
    Ended,
    TimedOut,
}
