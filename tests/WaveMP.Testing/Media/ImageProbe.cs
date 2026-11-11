using System.Diagnostics;
using System.Globalization;

namespace WaveMP.Testing.Media;

/// <summary>
/// Reads the size of a picture with ffprobe, e.g. to check scaled covers.
/// </summary>
public static class ImageProbe
{
    /// <summary>
    /// Width and height of the first video stream of the file.
    /// </summary>
    public static async Task<(int Width, int Height)> GetSizeAsync(string path)
    {
        ProcessStartInfo startInfo = new(FfmpegTool.FfprobePath)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        string[] arguments =
        [
            "-v", "error",
            "-select_streams", "v:0",
            "-show_entries", "stream=width,height",
            "-of", "csv=p=0:s=x",
            path,
        ];
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("ffprobe could not be started.");
        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffprobe failed for {path}: {await error}");
        }

        string[] parts = output.Trim().Split('x');
        return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Width and height of a picture given as bytes.
    /// </summary>
    public static async Task<(int Width, int Height)> GetSizeAsync(byte[] picture)
    {
        string path = Path.Combine(Path.GetTempPath(), "wavemp-tests", $"probe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, picture);
        try
        {
            return await GetSizeAsync(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
