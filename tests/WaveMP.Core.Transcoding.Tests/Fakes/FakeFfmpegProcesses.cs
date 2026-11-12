using System.Collections.Concurrent;
using WaveMP.Core.Transcoding.Processes;

namespace WaveMP.Core.Transcoding.Tests.Fakes;

/// <summary>
/// Starts fake ffmpeg processes that write segments and their playlist only when a test says so.
/// </summary>
internal sealed class FakeFfmpegProcesses : IFfmpegProcessFactory
{
    private readonly ConcurrentQueue<FakeFfmpegProcess> _started = new();

    public bool FailToStart { get; set; }

    public IReadOnlyList<FakeFfmpegProcess> Started => [.. _started];

    public FakeFfmpegProcess Last => Started[^1];

    public IFfmpegProcess Start(IReadOnlyList<string> arguments)
    {
        if (FailToStart)
        {
            throw new FfmpegStartException("ffmpeg is missing.");
        }

        FakeFfmpegProcess process = new(arguments);
        _started.Enqueue(process);
        return process;
    }
}
