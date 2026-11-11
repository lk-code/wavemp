namespace WaveMP.Service.Authentication;

/// <summary>
/// Claims of the session token and of the authenticated principal.
/// </summary>
public static class WaveMpClaimTypes
{
    /// <summary>
    /// Id of the profile (user) the session belongs to.
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// Id of the session.
    /// </summary>
    public const string SessionId = "sid";

    /// <summary>
    /// Role of the profile. Not part of the token: it is read from the database on every request, so a changed role
    /// applies at once.
    /// </summary>
    public const string Role = "role";
}
