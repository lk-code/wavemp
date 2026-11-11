namespace WaveMP.Core.Caching.Options;

/// <summary>
/// Settings of the cache (configuration section <c>Caching</c>, e.g. <c>Caching__FfmpegPath</c>). The cache lies in
/// the folder <c>cache</c> of the data folder, which comes from the environment variable <c>DATA_PATH</c> (default
/// <c>/data</c>, the volume of the container; relative paths are resolved against the content root). Its size limit is
/// a server setting, see <see cref="Settings.Groups.CacheSettings"/>.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Caching";
    public const string DataPathVariable = "DATA_PATH";
    public const string DefaultDataPath = "/data";
    public const string FolderName = "cache";

    /// <summary>
    /// Absolute path of the cache folder.
    /// </summary>
    public string RootPath { get; set; } = DefaultDataPath + "/" + FolderName;

    /// <summary>
    /// ffmpeg executable; a name is looked up in the PATH.
    /// </summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>
    /// Quality of the WebP pictures from 0 to 100.
    /// </summary>
    public int WebPQuality { get; set; } = 80;

    /// <summary>
    /// How many pictures are converted at the same time.
    /// </summary>
    public int MaxParallelEncodes { get; set; } = Math.Clamp(Environment.ProcessorCount / 2, 1, 4);

    /// <summary>
    /// How long ffmpeg may take for one picture before it is stopped.
    /// </summary>
    public TimeSpan EncodeTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How often the last use of a file is written to the file system (as its time of the last change), so that the
    /// order of the clean-up survives a restart.
    /// </summary>
    public TimeSpan UsageTouchInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Share of the size limit the clean-up reduces the cache to once it is exceeded; the preparation after a scan
    /// stops at this share as well.
    /// </summary>
    public double TrimTargetRatio { get; set; } = 0.9;

    /// <summary>
    /// How long a failed conversion of a picture is remembered before it is tried again.
    /// </summary>
    public TimeSpan FailureRetryDelay { get; set; } = TimeSpan.FromMinutes(10);
}
