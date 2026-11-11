using System.Diagnostics;

namespace WaveMP.Testing.Media;

/// <summary>
/// Runs ffmpeg for the synthetic test media. ffmpeg and ffprobe are taken from <c>FFMPEG_PATH</c> (the folder that
/// contains both) or from the PATH. Tests that need them fail with a clear message if they are missing, they are
/// never skipped.
/// </summary>
public static class FfmpegTool
{
    private static readonly Lazy<string> _ffmpeg = new(() => Find("ffmpeg"));
    private static readonly Lazy<string> _ffprobe = new(() => Find("ffprobe"));

    /// <summary>
    /// Full path of ffmpeg.
    /// </summary>
    public static string FfmpegPath => _ffmpeg.Value;

    /// <summary>
    /// Full path of ffprobe.
    /// </summary>
    public static string FfprobePath => _ffprobe.Value;

    /// <summary>
    /// Runs ffmpeg with the arguments and fails with its error output if it does not succeed.
    /// </summary>
    public static async Task RunAsync(params string[] arguments)
    {
        ProcessStartInfo startInfo = new(FfmpegPath)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string argument in (string[])["-hide_banner", "-loglevel", "error", "-nostdin", "-y", .. arguments])
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("ffmpeg could not be started.");
        Task<string> error = process.StandardError.ReadToEndAsync();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        await output;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"ffmpeg failed with exit code {process.ExitCode}: {await error}\nArguments: "
                + string.Join(' ', startInfo.ArgumentList));
        }
    }

    private static string Find(string tool)
    {
        string fileName = OperatingSystem.IsWindows() ? tool + ".exe" : tool;
        List<string> directories = [.. (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)];
        if (Environment.GetEnvironmentVariable("FFMPEG_PATH") is { Length: > 0 } own)
        {
            directories.Insert(0, own);
        }

        foreach (string directory in directories)
        {
            string candidate = Path.Combine(directory.Trim('"'), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"{tool} was not found. The media tests create their test files with ffmpeg: install ffmpeg (see README, "
            + "e.g. 'winget install --id Gyan.FFmpeg -e', 'apt install ffmpeg' or 'brew install ffmpeg') or set "
            + "FFMPEG_PATH to the folder that contains ffmpeg and ffprobe.");
    }
}
