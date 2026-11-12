using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Tests.Fakes;

internal static class TestVariants
{
    public static readonly TimeSpan SegmentDuration = TimeSpan.FromSeconds(2);

    public static TranscodeInput Input(
        TimeSpan? duration = null,
        int? width = 1920,
        int? height = 1080,
        bool hasAudio = true,
        Guid? fileId = null)
    {
        return new TranscodeInput(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            fileId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "/media/Artist/Song.mkv",
            123_456,
            new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero),
            duration ?? TimeSpan.FromSeconds(30),
            hasAudio,
            width,
            height);
    }

    public static TranscodeVariant Video(TranscodeInput? input = null, TranscodingSettings? settings = null)
    {
        TranscodeInput source = input ?? Input();
        return TranscodeVariant.Of(
            source,
            TranscodeProfiles.For(HlsMedia.Video, source, settings ?? new TranscodingSettings(), SegmentDuration));
    }

    public static TranscodeVariant Audio(TranscodeInput? input = null, TranscodingSettings? settings = null)
    {
        TranscodeInput source = input ?? Input(width: null, height: null);
        return TranscodeVariant.Of(
            source,
            TranscodeProfiles.For(HlsMedia.Audio, source, settings ?? new TranscodingSettings(), SegmentDuration));
    }
}
