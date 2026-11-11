using WaveMP.Core.Settings.Paths;

namespace WaveMP.Core.Playlists.Options;

/// <summary>
/// Settings of the playlists (configuration section <c>Playlists</c>, e.g. <c>Playlists__MaxEntries</c>). Uploaded
/// covers lie in the folder <c>playlists</c> of the data folder (see <see cref="DataPaths"/>).
/// </summary>
public sealed class PlaylistOptions
{
    public const string SectionName = "Playlists";
    public const string FolderName = "playlists";

    /// <summary>
    /// Absolute path of the folder of the uploaded covers, set from the data folder.
    /// </summary>
    public string RootPath { get; set; } = DataPaths.DefaultDataPath + "/" + FolderName;

    /// <summary>
    /// Largest number of entries of a playlist.
    /// </summary>
    public int MaxEntries { get; set; } = 10_000;

    /// <summary>
    /// Largest uploaded cover in bytes.
    /// </summary>
    public long MaxCoverBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// How many entries from the start of a playlist are searched for covers of its tracks.
    /// </summary>
    public int CoverScanEntries { get; set; } = 50;

    /// <summary>
    /// How many covers of its tracks a playlist without an own cover shows at most.
    /// </summary>
    public int CoverCount { get; set; } = 4;

    /// <summary>
    /// How many tracks the list of the recently played tracks holds at most.
    /// </summary>
    public int RecentlyPlayedMaxTracks { get; set; } = 100;
}
