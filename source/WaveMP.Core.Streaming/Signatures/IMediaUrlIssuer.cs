namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// Issues short-lived signed media URLs, for players that cannot send a token header: <c>&lt;audio&gt;</c>,
/// <c>&lt;video&gt;</c>, casting devices and external players.
/// </summary>
public interface IMediaUrlIssuer
{
    /// <summary>
    /// Signs one kind of media of a track for a user and session. The URL stays valid for the configured lifetime
    /// plus <paramref name="mediaDuration"/>, at most for the configured maximum.
    /// </summary>
    SignedMediaUrl Issue(
        MediaResource resource,
        Guid trackId,
        Guid userId,
        Guid sessionId,
        TimeSpan? mediaDuration = null);
}
