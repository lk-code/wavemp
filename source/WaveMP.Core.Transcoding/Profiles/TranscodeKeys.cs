using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WaveMP.Core.Transcoding.Profiles;

/// <summary>
/// The key of a transcode: a hash of the file version and everything that changes the segments. The encoder (hardware
/// or software) is not part of it, so a change of the encoder never invalidates the playlists of running players.
/// </summary>
public static class TranscodeKeys
{
    private const string Version = "v1";

    /// <summary>
    /// 32 lowercase hexadecimal characters.
    /// </summary>
    public static string Of(TranscodeInput input, TranscodeProfile profile)
    {
        string text = string.Join(
            "|",
            Version,
            input.FileId.ToString("N"),
            Invariant(input.Length),
            Invariant(input.LastModified.UtcTicks),
            Invariant(input.Duration.Ticks),
            profile.Media.ToString(),
            profile.Size is null ? "-" : Invariant(profile.Size.Width),
            profile.Size is null ? "-" : Invariant(profile.Size.Height),
            Invariant(profile.MaxHeight),
            Invariant(profile.VideoBitrateKbps),
            Invariant(profile.AudioBitrateKbps),
            Invariant(profile.SegmentDuration.Ticks));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexStringLower(hash, 0, 16);
    }

    private static string Invariant(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
