namespace WaveMP.Core.Streaming.Options;

/// <summary>
/// Settings of the streaming (configuration section <c>Streaming</c>, e.g. <c>Streaming__SignedUrlLifetime</c>).
/// </summary>
public sealed class StreamingOptions
{
    public const string SectionName = "Streaming";

    /// <summary>
    /// How long a signed media URL stays valid after it was issued, on top of the duration of the track, so range
    /// requests keep working while the track plays.
    /// </summary>
    public TimeSpan SignedUrlLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Upper limit of the validity of a signed media URL, also for very long tracks.
    /// </summary>
    public TimeSpan MaxSignedUrlLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// How far the clock of a client may be ahead of the server, e.g. for the start of a play in the history.
    /// </summary>
    public TimeSpan MaxClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How old a play may be when the client reports it, e.g. after the app was offline.
    /// </summary>
    public TimeSpan HistoryMaxAge { get; set; } = TimeSpan.FromDays(7);
}
