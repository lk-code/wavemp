namespace WaveMP.Service.Signatures;

/// <summary>
/// Fixed values of the signed media URLs: the authentication scheme and the names of their query parameters.
/// </summary>
public static class MediaSignatureDefaults
{
    /// <summary>
    /// Name of the authentication scheme and of the security scheme in the OpenAPI document.
    /// </summary>
    public const string AuthenticationScheme = "MediaSignature";

    /// <summary>
    /// Id of the user the URL was issued to.
    /// </summary>
    public const string UserIdParameter = "uid";

    /// <summary>
    /// Id of the session the URL was issued in.
    /// </summary>
    public const string SessionIdParameter = "sid";

    /// <summary>
    /// Expiry of the URL in Unix seconds.
    /// </summary>
    public const string ExpiresParameter = "exp";

    /// <summary>
    /// Base64Url-encoded signature.
    /// </summary>
    public const string SignatureParameter = "sig";

    /// <summary>
    /// Name of the route value with the id of the track.
    /// </summary>
    public const string TrackIdRouteValue = "id";
}
