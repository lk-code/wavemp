namespace WaveMP.Data.Entities;

/// <summary>
/// Settings of a user, one row per user. Later settings are added as further columns.
/// </summary>
public class UserSettings
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Language Language { get; set; } = Language.En;

    /// <summary>
    /// Volume normalization of the player; off by default.
    /// </summary>
    public ReplayGainMode ReplayGain { get; set; } = ReplayGainMode.Off;
}
