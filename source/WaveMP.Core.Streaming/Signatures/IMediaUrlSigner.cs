namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// Signs and checks the claims of media URLs.
/// </summary>
public interface IMediaUrlSigner
{
    /// <summary>
    /// The signature of the claims, Base64Url-encoded.
    /// </summary>
    string Sign(MediaUrlClaims claims);

    /// <summary>
    /// Checks a signature against the claims at the time <paramref name="now"/>.
    /// </summary>
    MediaUrlVerification Verify(MediaUrlClaims claims, string? signature, DateTimeOffset now);
}
