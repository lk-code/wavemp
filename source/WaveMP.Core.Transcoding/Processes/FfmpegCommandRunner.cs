namespace WaveMP.Core.Transcoding.Processes;

internal sealed class FfmpegCommandRunner(IFfmpegProcessFactory processes) : IFfmpegCommandRunner
{
    public async Task<FfmpegCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        IFfmpegProcess process;
        try
        {
            process = processes.Start(arguments);
        }
        catch (FfmpegStartException)
        {
            return FfmpegCommandResult.NotStarted;
        }

        using (process)
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limit.CancelAfter(timeout);
            try
            {
                int exitCode = await process.Exited.WaitAsync(limit.Token);
                return new FfmpegCommandResult(true, exitCode, process.ErrorOutput);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                cancellationToken.ThrowIfCancellationRequested();
                return new FfmpegCommandResult(true, -1, process.ErrorOutput);
            }
        }
    }
}
