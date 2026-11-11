namespace WaveMP.Service.Tokens;

/// <summary>
/// Names of the secrets the service keeps in the database.
/// </summary>
public static class ServerSecretNames
{
    /// <summary>
    /// Key that signs the session tokens.
    /// </summary>
    public const string JwtSigningKey = "jwt-signing-key";
}
