using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WaveMP.Core.Settings.Paths;

/// <summary>
/// The data folder of WaveMP: everything that has to survive a restart lies there (cache, uploaded pictures,
/// backups). It comes from the environment variable <c>DATA_PATH</c>, by default <c>/data</c>, the volume of the
/// container; a relative path is resolved against the content root.
/// </summary>
public static class DataPaths
{
    public const string DataPathVariable = "DATA_PATH";
    public const string DefaultDataPath = "/data";

    /// <summary>
    /// The absolute path of the data folder.
    /// </summary>
    /// <param name="configuration">Configuration with <c>DATA_PATH</c>.</param>
    /// <param name="environment">Host environment whose content root resolves relative paths; without it, the
    /// current folder.</param>
    public static string Resolve(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        string dataPath = configuration[DataPathVariable] is { Length: > 0 } configured
            ? configured
            : DefaultDataPath;
        string contentRoot = environment?.ContentRootPath ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(dataPath, contentRoot);
    }

    /// <summary>
    /// The absolute path of a folder in the data folder, e.g. <c>cache</c>.
    /// </summary>
    public static string Resolve(IConfiguration configuration, IHostEnvironment? environment, string folderName)
    {
        return Path.Combine(Resolve(configuration, environment), folderName);
    }
}
