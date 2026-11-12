namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// The encoder of transcoded videos.
/// </summary>
public enum HardwareAccelerator
{
    /// <summary>
    /// Software (libx264); always available.
    /// </summary>
    None,

    /// <summary>
    /// Intel graphics through VA-API (<c>h264_vaapi</c>).
    /// </summary>
    Vaapi,

    /// <summary>
    /// Intel Quick Sync Video (<c>h264_qsv</c>).
    /// </summary>
    Qsv,
}
