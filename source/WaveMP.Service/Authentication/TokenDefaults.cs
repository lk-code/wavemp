using Microsoft.IdentityModel.Tokens;

namespace WaveMP.Service.Authentication;

/// <summary>
/// Fixed values of the session tokens (JWT).
/// </summary>
public static class TokenDefaults
{
    public const string Issuer = "wavemp";

    public const string Audience = "wavemp";

    public const string Algorithm = SecurityAlgorithms.HmacSha256;

    /// <summary>
    /// Name of the security scheme in the OpenAPI document.
    /// </summary>
    public const string SecuritySchemeName = "Bearer";
}
