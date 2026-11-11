using System.Text.Json;
using Quartz;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Scans;

namespace WaveMP.Core.Scanning.Jobs;

/// <summary>
/// The library scan as background job: scans the whole media folder, or the folders named in the payload (a JSON
/// list of relative paths, sent by the file watcher). Never runs in parallel to itself; every run is built in a
/// dependency injection scope of its own. The result becomes part of the run history.
/// </summary>
[DisallowConcurrentExecution]
public sealed class LibraryScanJob(ILibraryScanner scanner, IJobRunTracker tracker) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        Guid runId = tracker.FindRunId(context.FireInstanceId) ?? Guid.CreateVersion7();
        ScanScope scope = ReadScope(context.MergedJobDataMap);
        context.Result = await scanner.ScanAsync(scope, runId, cancellationToken);
    }

    /// <summary>
    /// The folders of the payload, or the whole media folder.
    /// </summary>
    internal static ScanScope ReadScope(JobDataMap data)
    {
        if (!data.TryGetValue(JobDataKeys.Payload, out object? value) || value is not string { Length: > 0 } json)
        {
            return ScanScope.Full;
        }

        try
        {
            string[]? directories = JsonSerializer.Deserialize<string[]>(json);
            return directories is { Length: > 0 } ? ScanScope.Of(directories) : ScanScope.Full;
        }
        catch (JsonException)
        {
            return ScanScope.Full;
        }
    }

    /// <summary>
    /// The payload for a scan of the folders.
    /// </summary>
    public static string CreatePayload(ScanScope scope)
    {
        return JsonSerializer.Serialize(scope.Directories);
    }
}
