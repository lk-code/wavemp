using WaveMP.Core.Transcoding.Arguments;
using WaveMP.Core.Transcoding.Processes;

namespace WaveMP.Core.Transcoding.Tests.Fakes;

/// <summary>
/// A fake ffmpeg process; the test writes its segments and ends it.
/// </summary>
internal sealed class FakeFfmpegProcess(IReadOnlyList<string> arguments) : IFfmpegProcess
{
    public const int KilledExitCode = 137;

    private readonly TaskCompletionSource<int> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<string> _segments = [];

    public IReadOnlyList<string> Arguments { get; } = arguments;

    public string Folder => Path.GetDirectoryName(Arguments[^1])!;

    public bool Killed { get; private set; }

    public bool Disposed { get; private set; }

    public Task<int> Exited => _exited.Task;

    public string ErrorOutput { get; set; } = string.Empty;

    /// <summary>
    /// Value of an option, e.g. <c>-start_number</c>.
    /// </summary>
    public string? ValueOf(string option)
    {
        int index = Arguments.ToList().IndexOf(option);
        return index >= 0 && index + 1 < Arguments.Count ? Arguments[index + 1] : null;
    }

    /// <summary>
    /// Writes a finished segment and lists it in the playlist, like ffmpeg does.
    /// </summary>
    public void Produce(int index, byte[]? content = null)
    {
        string name = $"{FfmpegHlsArguments.SegmentPrefix}{index:D5}.ts";
        File.WriteAllBytes(Path.Combine(Folder, name), content ?? ContentOf(index));
        _segments.Add(name);
        string playlist = "#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-TARGETDURATION:2\n"
            + string.Concat(_segments.Select(segment => $"#EXTINF:2.000000,\n{segment}\n"));
        File.WriteAllText(Path.Combine(Folder, FfmpegHlsArguments.PlaylistName), playlist);
    }

    public void Exit(int exitCode)
    {
        _exited.TrySetResult(exitCode);
    }

    public void Kill()
    {
        if (_exited.Task.IsCompleted)
        {
            return;
        }

        Killed = true;
        _exited.TrySetResult(KilledExitCode);
    }

    public void Dispose()
    {
        Disposed = true;
        Kill();
    }

    public static byte[] ContentOf(int index)
    {
        return [0x47, (byte)index, 1, 2, 3];
    }
}
