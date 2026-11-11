namespace WaveMP.Service.Signatures;

/// <summary>
/// A signed media URL.
/// </summary>
/// <param name="Url">Root-relative URL with the signature in its query.</param>
/// <param name="ExpiresAt">When the URL expires.</param>
public sealed record MediaUrl(string Url, DateTimeOffset ExpiresAt);
