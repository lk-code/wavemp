namespace WaveMP.Core.Streaming.Offsets;

/// <summary>
/// Limits of the offset of the video against the audio of a track.
/// </summary>
public static class VideoOffsets
{
    /// <summary>
    /// Smallest offset in milliseconds: the music in the video starts ten minutes earlier than in the audio file.
    /// </summary>
    public const int MinMilliseconds = -600_000;

    /// <summary>
    /// Largest offset in milliseconds: the music in the video starts ten minutes later than in the audio file.
    /// </summary>
    public const int MaxMilliseconds = 600_000;

    public static bool IsValid(int milliseconds)
    {
        return milliseconds is >= MinMilliseconds and <= MaxMilliseconds;
    }
}
