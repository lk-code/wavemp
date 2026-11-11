namespace WaveMP.Service.Tokens;

/// <summary>
/// Names of the authorization policies.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Only profiles with the role admin.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Endpoints that deliver media: a session token or a signed media URL (see
    /// <see cref="Signatures.SignedMediaAttribute"/>).
    /// </summary>
    public const string Media = "Media";
}
