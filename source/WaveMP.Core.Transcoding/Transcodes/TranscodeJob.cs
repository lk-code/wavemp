using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Processes;
using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// One ffmpeg process that transcodes a variant from a start segment on into its temporary folder.
/// </summary>
internal sealed class TranscodeJob(
    Guid id,
    TranscodeVariant variant,
    int startSegment,
    HardwareAccelerator accelerator,
    string folder,
    IFfmpegProcess process,
    Guid? userId,
    DateTimeOffset startedAt)
{
    private long _lastAccess = startedAt.UtcTicks;
    private int _nextSegment = startSegment;
    private int _state = (int)TranscodeJobState.Running;
    private int _stopRequested;
    private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Guid Id { get; } = id;

    public TranscodeVariant Variant { get; } = variant;

    public string Key => Variant.Id;

    public int StartSegment { get; } = startSegment;

    public HardwareAccelerator Accelerator { get; } = accelerator;

    public string Folder { get; } = folder;

    public IFfmpegProcess Process { get; } = process;

    public Guid? UserId { get; } = userId;

    public DateTimeOffset StartedAt { get; } = startedAt;

    /// <summary>
    /// The watcher of the process; set right after the start.
    /// </summary>
    public Task Watcher { get; set; } = Task.CompletedTask;

    public DateTimeOffset LastAccess => new(Interlocked.Read(ref _lastAccess), TimeSpan.Zero);

    /// <summary>
    /// Index of the next segment that is not in the cache yet.
    /// </summary>
    public int NextSegment => Volatile.Read(ref _nextSegment);

    public TranscodeJobState State => (TranscodeJobState)Volatile.Read(ref _state);

    public bool HasEnded => State != TranscodeJobState.Running;

    public bool StopRequested => Volatile.Read(ref _stopRequested) == 1;

    /// <summary>
    /// Whether the job runs and was not asked to stop; only active jobs take a slot and get new requests.
    /// </summary>
    public bool IsActive => !HasEnded && !StopRequested;

    public bool ProducedAny => NextSegment > StartSegment;

    /// <summary>
    /// Whether a request for the segment should wait for this job: the segment is next or close ahead.
    /// </summary>
    public bool Covers(int index, int aheadSegments)
    {
        int next = NextSegment;
        return index >= next && index < next + Math.Max(1, aheadSegments);
    }

    public void Touch(DateTimeOffset now)
    {
        Interlocked.Exchange(ref _lastAccess, now.UtcTicks);
    }

    public void Advance(int nextSegment)
    {
        Volatile.Write(ref _nextSegment, nextSegment);
        Pulse();
    }

    /// <summary>
    /// Asks the process to stop; the watcher finishes the job.
    /// </summary>
    public void Stop()
    {
        Volatile.Write(ref _stopRequested, 1);
        Process.Kill();
    }

    public void Finish(TranscodeJobState state)
    {
        Volatile.Write(ref _state, (int)state);
        Pulse();
    }

    /// <summary>
    /// Waits until the segment is in the cache, the job has ended or the time is up.
    /// </summary>
    public async Task<SegmentWait> WaitForSegmentAsync(
        int index,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        while (true)
        {
            Task changed = Volatile.Read(ref _changed).Task;
            if (NextSegment > index)
            {
                return SegmentWait.Ready;
            }

            if (HasEnded)
            {
                return SegmentWait.Ended;
            }

            try
            {
                await changed.WaitAsync(limit.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return SegmentWait.TimedOut;
            }
        }
    }

    public TranscodeJobInfo ToInfo()
    {
        return new TranscodeJobInfo(
            Id,
            Variant.Input.TrackId,
            Variant.Media,
            Accelerator,
            StartSegment,
            NextSegment,
            Variant.SegmentCount,
            StartedAt,
            LastAccess,
            UserId);
    }

    private void Pulse()
    {
        TaskCompletionSource previous = Interlocked.Exchange(
            ref _changed,
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        previous.TrySetResult();
    }
}
