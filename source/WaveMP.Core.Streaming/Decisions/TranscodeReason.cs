namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Why a track has to be transcoded.
/// </summary>
public enum TranscodeReason
{
    /// <summary>
    /// The client does not support the format of the file.
    /// </summary>
    FormatUnsupported,

    /// <summary>
    /// The format of the file is not one clients can play directly (see <see cref="Formats.AudioFormats"/>).
    /// </summary>
    FormatUnknown,
}
