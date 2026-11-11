using System.Globalization;

namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// The version of an own cover: the time of its upload as text, part of its entity tag.
/// </summary>
public static class PlaylistCoverVersions
{
    public static string Of(DateTimeOffset coverUpdatedAt)
    {
        return coverUpdatedAt.UtcTicks.ToString("x", CultureInfo.InvariantCulture);
    }
}
