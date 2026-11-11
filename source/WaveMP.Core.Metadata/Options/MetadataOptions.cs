namespace WaveMP.Core.Metadata.Options;

/// <summary>
/// Settings of the metadata reader (configuration section <c>Metadata</c>, e.g. <c>Metadata__FfprobePath</c>).
/// </summary>
public sealed class MetadataOptions
{
    public const string SectionName = "Metadata";

    /// <summary>
    /// ffprobe executable; a name is looked up in the PATH.
    /// </summary>
    public string FfprobePath { get; set; } = "ffprobe";

    /// <summary>
    /// How long ffprobe may take for one file before it is stopped.
    /// </summary>
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
