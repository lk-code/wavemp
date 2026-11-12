namespace WaveMP.Core.Transcoding.Tests.Fakes;

/// <summary>
/// A clock the test moves; timers and delays still run on the real time.
/// </summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }

    public void Advance(TimeSpan duration)
    {
        _now += duration;
    }
}
