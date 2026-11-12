using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// What a client plays directly in a video element: containers, codecs of the picture and codecs of the sound. Empty
/// sets mean that the client only plays HLS.
/// </summary>
public sealed record VideoCapabilities(
    IReadOnlySet<VideoContainer> Containers,
    IReadOnlySet<VideoCodec> VideoCodecs,
    IReadOnlySet<AudioCodec> AudioCodecs)
{
    /// <summary>
    /// A client that plays nothing directly, only HLS.
    /// </summary>
    public static VideoCapabilities None { get; } = new(
        new HashSet<VideoContainer>(),
        new HashSet<VideoCodec>(),
        new HashSet<AudioCodec>());
}
