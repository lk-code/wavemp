using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Options;

namespace WaveMP.Core.Caching.Images;

/// <summary>
/// Converts covers with ffmpeg as a separate process (never linked). The picture is passed through standard input,
/// the arguments one by one without a shell. At most <see cref="CacheOptions.MaxParallelEncodes"/> conversions run at
/// the same time.
/// </summary>
internal sealed class FfmpegCoverImageEncoder(
    IOptions<CacheOptions> options,
    ILogger<FfmpegCoverImageEncoder> logger) : ICoverImageEncoder, IDisposable
{
    private readonly SemaphoreSlim _slots = new(Math.Max(1, options.Value.MaxParallelEncodes));

    public async Task<CoverEncodeStatus> EncodeAsync(
        Stream source,
        string contentType,
        IReadOnlyList<CoverEncodeTarget> targets,
        CancellationToken cancellationToken = default)
    {
        if (targets.Count == 0)
        {
            return CoverEncodeStatus.Encoded;
        }

        if (FfmpegCoverArguments.DemuxerOf(contentType) is not { } demuxer)
        {
            return CoverEncodeStatus.Failed;
        }

        await _slots.WaitAsync(cancellationToken);
        try
        {
            return await RunAsync(source, demuxer, targets, cancellationToken);
        }
        finally
        {
            _slots.Release();
        }
    }

    public void Dispose()
    {
        _slots.Dispose();
    }

    private async Task<CoverEncodeStatus> RunAsync(
        Stream source,
        string demuxer,
        IReadOnlyList<CoverEncodeTarget> targets,
        CancellationToken cancellationToken)
    {
        CacheOptions settings = options.Value;
        ProcessStartInfo startInfo = new(settings.FfmpegPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in FfmpegCoverArguments.Build(demuxer, targets, settings.WebPQuality))
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            logger.LogWarning(
                exception,
                "ffmpeg ({FfmpegPath}) cannot be started; covers are delivered unscaled",
                settings.FfmpegPath);
            return CoverEncodeStatus.Unavailable;
        }

        if (process is null)
        {
            return CoverEncodeStatus.Unavailable;
        }

        using (process)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(settings.EncodeTimeout);
            Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
            Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            try
            {
                await WriteInputAsync(process, source, timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                string errorText = await error;
                await output;

                if (process.ExitCode == 0 && targets.All(target => IsWritten(target.OutputPath)))
                {
                    return CoverEncodeStatus.Encoded;
                }

                logger.LogDebug(
                    "ffmpeg cannot convert a cover (exit code {ExitCode}): {Error}",
                    process.ExitCode,
                    errorText.Trim());
            }
            catch (OperationCanceledException)
            {
                Stop(process);
                DeleteTargets(targets);
                cancellationToken.ThrowIfCancellationRequested();
                logger.LogWarning("ffmpeg took longer than {Timeout} to convert a cover", settings.EncodeTimeout);
                return CoverEncodeStatus.Failed;
            }

            DeleteTargets(targets);
            return CoverEncodeStatus.Failed;
        }
    }

    private static async Task WriteInputAsync(Process process, Stream source, CancellationToken cancellationToken)
    {
        Stream input = process.StandardInput.BaseStream;
        try
        {
            await source.CopyToAsync(input, cancellationToken);
            await input.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
            // ffmpeg stopped reading, e.g. after an error in the picture; its exit code tells.
        }
        finally
        {
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // Already closed by ffmpeg.
            }
        }
    }

    private static bool IsWritten(string path)
    {
        FileInfo file = new(path);
        return file.Exists && file.Length > 0;
    }

    private void DeleteTargets(IEnumerable<CoverEncodeTarget> targets)
    {
        foreach (CoverEncodeTarget target in targets)
        {
            try
            {
                File.Delete(target.OutputPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(exception, "Cannot remove the unfinished picture {Path}", target.OutputPath);
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
}
