namespace WaveMP.Core.Jobs.Definitions;

/// <summary>
/// Why a schedule is created.
/// </summary>
public enum JobScheduleReason
{
    /// <summary>
    /// The service starts.
    /// </summary>
    Startup,

    /// <summary>
    /// The settings of the schedule changed.
    /// </summary>
    SettingsChanged,
}
