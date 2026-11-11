namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// What a running scan is doing.
/// </summary>
public enum ScanPhase
{
    /// <summary>
    /// Walks the media folder and compares it with the library.
    /// </summary>
    Discovering,

    /// <summary>
    /// Reads new and changed files and updates their folders.
    /// </summary>
    Processing,

    /// <summary>
    /// Removes what is gone and updates album covers and totals.
    /// </summary>
    CleaningUp,
}
