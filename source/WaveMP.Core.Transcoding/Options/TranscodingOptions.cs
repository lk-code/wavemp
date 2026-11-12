namespace WaveMP.Core.Transcoding.Options;

/// <summary>
/// Technical settings of the transcoding (configuration section <c>Transcoding</c>, e.g.
/// <c>Transcoding__RenderDevice</c>). What an admin changes in the web app (hardware on or off, parallel jobs, quality)
/// is a server setting, see <see cref="Settings.Groups.TranscodingSettings"/>.
/// </summary>
public sealed class TranscodingOptions
{
    public const string SectionName = "Transcoding";

    /// <summary>
    /// ffmpeg executable; a name is looked up in the PATH.
    /// </summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>
    /// Render device of the graphics hardware (e.g. <c>/dev/dri/renderD128</c>); <c>null</c> takes the first
    /// <c>renderD*</c> device of <see cref="RenderDeviceFolder"/>.
    /// </summary>
    public string? RenderDevice { get; set; }

    /// <summary>
    /// Folder of the render devices that is searched without <see cref="RenderDevice"/>.
    /// </summary>
    public string RenderDeviceFolder { get; set; } = "/dev/dri";

    /// <summary>
    /// Length of an HLS segment.
    /// </summary>
    public TimeSpan SegmentDuration { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>
    /// A transcode that no client asked for this long is stopped.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// With all slots taken, a transcode no client asked for this long gives its slot to a new one.
    /// </summary>
    public TimeSpan EvictIdleAfter { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long a request waits for its segment before it answers with a retry.
    /// </summary>
    public TimeSpan SegmentWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// A segment further ahead of a running transcode than this starts a new transcode at the segment (a seek)
    /// instead of waiting.
    /// </summary>
    public TimeSpan RestartAhead { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// How long one test of the hardware may take.
    /// </summary>
    public TimeSpan DetectionTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Preset of the software encoder (libx264).
    /// </summary>
    public string X264Preset { get; set; } = "veryfast";

    /// <summary>
    /// How often a running transcode is checked for finished segments.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// How often idle transcodes are looked for.
    /// </summary>
    public TimeSpan JanitorInterval { get; set; } = TimeSpan.FromSeconds(10);
}
