namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Whether the transcoding uses the graphics hardware. Stored as its name.
/// </summary>
public enum HardwareAccelerationMode
{
    /// <summary>
    /// Intel Quick Sync (QSV or VAAPI) is detected on start and used if it works; otherwise software.
    /// </summary>
    Auto,

    /// <summary>
    /// Always software.
    /// </summary>
    Off,
}
