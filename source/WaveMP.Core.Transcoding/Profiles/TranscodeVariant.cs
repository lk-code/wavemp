namespace WaveMP.Core.Transcoding.Profiles;

/// <summary>
/// One HLS variant of a track: the file, the profile and its segments. Its <see cref="Id"/> changes with the file
/// version and the profile, so playlists of an older variant are recognized as outdated.
/// </summary>
/// <param name="Id">Key of the variant (see <see cref="TranscodeKeys"/>); also the folder of its segments in the cache.
/// </param>
/// <param name="Input">The file.</param>
/// <param name="Profile">What is produced.</param>
public sealed record TranscodeVariant(string Id, TranscodeInput Input, TranscodeProfile Profile)
{
    /// <summary>
    /// A rest shorter than this at the end gets no segment of its own in the playlist; it is part of the segment
    /// before.
    /// </summary>
    public static readonly TimeSpan EndTolerance = TimeSpan.FromSeconds(0.5);

    public HlsMedia Media => Profile.Media;

    /// <summary>
    /// The codecs for the master playlist: H.264 High with the level the height needs, and AAC-LC.
    /// </summary>
    public string Codecs
    {
        get
        {
            const string Aac = "mp4a.40.2";
            if (Media == HlsMedia.Audio)
            {
                return Aac;
            }

            int height = Profile.Size?.Height ?? Profile.MaxHeight;
            string video = height switch
            {
                <= 1080 => "avc1.64002a",
                <= 1440 => "avc1.640032",
                _ => "avc1.640033",
            };
            return Input.HasAudio ? video + "," + Aac : video;
        }
    }

    /// <summary>
    /// Peak bandwidth for the master playlist in bit/s.
    /// </summary>
    public int Bandwidth => Input.HasAudio || Media == HlsMedia.Audio
        ? Profile.Bandwidth
        : Profile.VideoBitrateKbps * 3 / 2 * 1100;

    /// <summary>
    /// Number of segments; at least one.
    /// </summary>
    public int SegmentCount => CountOf(Input.Duration, Profile.SegmentDuration);

    public static TranscodeVariant Of(TranscodeInput input, TranscodeProfile profile)
    {
        return new TranscodeVariant(TranscodeKeys.Of(input, profile), input, profile);
    }

    public static int CountOf(TimeSpan duration, TimeSpan segmentDuration)
    {
        if (segmentDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentDuration), segmentDuration, "Must be positive.");
        }

        double usable = (duration - EndTolerance).TotalSeconds;
        return Math.Max(1, (int)Math.Ceiling(usable / segmentDuration.TotalSeconds));
    }

    /// <summary>
    /// Duration of the segment; the last one reaches to the end of the file.
    /// </summary>
    public TimeSpan DurationOf(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, SegmentCount);
        if (index < SegmentCount - 1)
        {
            return Profile.SegmentDuration;
        }

        TimeSpan rest = Input.Duration - StartOf(index);
        return rest > TimeSpan.Zero ? rest : Profile.SegmentDuration;
    }

    /// <summary>
    /// Start of the segment in the file.
    /// </summary>
    public TimeSpan StartOf(int index)
    {
        return Profile.SegmentDuration * index;
    }

    /// <summary>
    /// Name of the segment in the cache area <c>Transcodes</c>.
    /// </summary>
    public string SegmentNameOf(int index)
    {
        return $"{Id}/{index:D5}.ts";
    }
}
