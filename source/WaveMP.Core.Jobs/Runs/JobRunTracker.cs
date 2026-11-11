namespace WaveMP.Core.Jobs.Runs;

/// <summary>
/// Keeps the queued and running runs of every job in memory (singleton).
/// </summary>
public sealed class JobRunTracker : IJobRunTracker
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, JobActivity> _jobs = new(StringComparer.Ordinal);

    public JobActivityState GetState(string jobId)
    {
        return GetActiveRun(jobId)?.State ?? JobActivityState.Idle;
    }

    public JobRunActivity? GetActiveRun(string jobId)
    {
        lock (_lock)
        {
            if (!_jobs.TryGetValue(jobId, out JobActivity? activity))
            {
                return null;
            }

            if (activity.Running is { } running)
            {
                return new JobRunActivity(running, JobActivityState.Running);
            }

            return activity.Queued.Count > 0 ? new JobRunActivity(activity.Queued[0], JobActivityState.Queued) : null;
        }
    }

    public bool TryReserve(string jobId, Guid runId)
    {
        lock (_lock)
        {
            JobActivity activity = Get(jobId);
            if (activity.Running is not null || activity.Queued.Count > 0)
            {
                return false;
            }

            activity.Queued.Add(runId);
            return true;
        }
    }

    public void MarkRunning(string jobId, Guid runId, string fireInstanceId)
    {
        lock (_lock)
        {
            JobActivity activity = Get(jobId);
            activity.Queued.Remove(runId);
            activity.Running = runId;
            activity.FireInstanceId = fireInstanceId;
        }
    }

    public Guid? FindRunId(string fireInstanceId)
    {
        lock (_lock)
        {
            return _jobs.Values.FirstOrDefault(activity => activity.FireInstanceId == fireInstanceId)?.Running;
        }
    }

    public void Release(string jobId, Guid runId)
    {
        lock (_lock)
        {
            if (!_jobs.TryGetValue(jobId, out JobActivity? activity))
            {
                return;
            }

            activity.Queued.Remove(runId);
            if (activity.Running == runId)
            {
                activity.Running = null;
                activity.FireInstanceId = null;
            }
        }
    }

    private JobActivity Get(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out JobActivity? activity))
        {
            activity = new JobActivity();
            _jobs[jobId] = activity;
        }

        return activity;
    }

    private sealed class JobActivity
    {
        public List<Guid> Queued { get; } = [];

        public Guid? Running { get; set; }

        public string? FireInstanceId { get; set; }
    }
}
