using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using WaveMP.Core.Transcoding.Options;

namespace WaveMP.Core.Transcoding.Processes;

/// <summary>
/// Starts ffmpeg from <see cref="TranscodingOptions.FfmpegPath"/> without a shell and without a window. The output is
/// read continuously, so the process never blocks on a full pipe; the end of the error output is kept for the log.
/// </summary>
internal sealed class FfmpegProcessFactory(IOptions<TranscodingOptions> options) : IFfmpegProcessFactory
{
    public IFfmpegProcess Start(IReadOnlyList<string> arguments)
    {
        string ffmpeg = options.Value.FfmpegPath;
        ProcessStartInfo startInfo = new(ffmpeg)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process = new() { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
            {
                process.Dispose();
                throw new FfmpegStartException($"ffmpeg ({ffmpeg}) did not start.");
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            throw new FfmpegStartException($"ffmpeg ({ffmpeg}) cannot be started.", exception);
        }

        return new FfmpegProcess(process);
    }

    private sealed class FfmpegProcess : IFfmpegProcess
    {
        private const int MaxErrorLength = 4000;

        private readonly Process _process;
        private readonly StringBuilder _error = new();
        private readonly Lock _lock = new();

        public FfmpegProcess(Process process)
        {
            _process = process;
            process.ErrorDataReceived += (_, line) => Append(line.Data);
            process.OutputDataReceived += (_, _) => { };
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            try
            {
                // ffmpeg runs with -nostdin; a closed input ends any question it might still ask.
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // Already gone.
            }

            Exited = WaitAsync(process);
        }

        public Task<int> Exited { get; }

        public string ErrorOutput
        {
            get
            {
                lock (_lock)
                {
                    return _error.ToString().Trim();
                }
            }
        }

        public void Kill()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                // Ended in the meantime.
            }
        }

        public void Dispose()
        {
            Kill();
            _process.Dispose();
        }

        private static async Task<int> WaitAsync(Process process)
        {
            await process.WaitForExitAsync();
            return process.ExitCode;
        }

        private void Append(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (_lock)
            {
                _error.AppendLine(line);
                if (_error.Length > MaxErrorLength)
                {
                    _error.Remove(0, _error.Length - MaxErrorLength);
                }
            }
        }
    }
}
