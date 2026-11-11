namespace WaveMP.Core.Jobs.Options;

/// <summary>
/// How long the run history is kept (configuration section <c>Jobs:History</c>). Older runs are removed after every
/// run of the same job.
/// </summary>
public sealed class JobHistoryOptions
{
    public const string SectionName = "Jobs:History";

    /// <summary>
    /// Finished runs older than this are removed.
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// At most this many runs are kept per job.
    /// </summary>
    public int MaxRunsPerJob { get; set; } = 100;
}
