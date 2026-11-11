using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Metadata.Options;

namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// Runs ffprobe as a separate process (never linked) and reads its JSON output. The path is passed as an argument of
/// its own, without a shell.
/// </summary>
internal sealed class FfprobeMediaProber(IOptions<MetadataOptions> options, ILogger<FfprobeMediaProber> logger)
    : IMediaProber
{
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ProcessResult? result = await RunAsync(["-version"], cancellationToken);
            return result is { ExitCode: 0 };
        }
        catch (ProbeUnavailableException)
        {
            return false;
        }
    }

    public async Task<MediaProbeResult?> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        ProcessResult? result = await RunAsync(
            ["-v", "error", "-print_format", "json", "-show_format", "-show_streams", path],
            cancellationToken);
        if (result is null)
        {
            logger.LogWarning("ffprobe took too long for {Path}", path);
            return null;
        }

        if (result.ExitCode != 0)
        {
            logger.LogDebug("ffprobe failed for {Path}: {Error}", path, result.Error.Trim());
            return null;
        }

        return FfprobeOutputParser.Parse(result.Output);
    }

    /// <summary>
    /// Runs ffprobe; <c>null</c> if it took longer than the timeout.
    /// </summary>
    private async Task<ProcessResult?> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        MetadataOptions settings = options.Value;
        ProcessStartInfo startInfo = new(settings.FfprobePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new ProbeUnavailableException($"ffprobe ({settings.FfprobePath}) could not be started.");
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new ProbeUnavailableException(
                $"ffprobe ({settings.FfprobePath}) could not be started: {exception.Message}",
                exception);
        }

        using (process)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(settings.ProbeTimeout);
            Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                return new ProcessResult(process.ExitCode, await output, await error);
            }
            catch (OperationCanceledException)
            {
                Stop(process);
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
        }
    }

    private static void Stop(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Ended in the meantime.
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
