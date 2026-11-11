namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// Result of the check of a signed media URL.
/// </summary>
public enum MediaUrlVerification
{
    Valid,

    /// <summary>
    /// The signature is valid, but the URL has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// The signature does not match the claims: a changed URL, another key or a malformed signature.
    /// </summary>
    Invalid,

    /// <summary>
    /// The URL expires later than any URL the server issues.
    /// </summary>
    TooFarInFuture,
}
