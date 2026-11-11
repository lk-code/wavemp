namespace WaveMP.Core.Users.Sessions;

/// <summary>
/// Settings of the sessions created by selecting a profile.
/// </summary>
public sealed class SessionOptions
{
    /// <summary>
    /// How long a session and its token stay valid. Profiles have no password, so a short lifetime would add no
    /// security; the selection is remembered until the profile is switched.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(365);
}
