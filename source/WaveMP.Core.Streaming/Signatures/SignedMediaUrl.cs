namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// The claims of a media URL with their signature. The service turns them into the query of the URL.
/// </summary>
/// <param name="Claims">What the URL grants.</param>
/// <param name="Signature">Base64Url-encoded signature of the claims.</param>
public sealed record SignedMediaUrl(MediaUrlClaims Claims, string Signature);
