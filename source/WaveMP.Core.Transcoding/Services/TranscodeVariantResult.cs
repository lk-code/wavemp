using System.Diagnostics.CodeAnalysis;
using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Services;

/// <summary>
/// The HLS variant of a track, or why there is none.
/// </summary>
/// <param name="Status">Whether the variant exists.</param>
/// <param name="Variant">The variant; set if found.</param>
public sealed record TranscodeVariantResult(TranscodeVariantStatus Status, TranscodeVariant? Variant)
{
    [MemberNotNullWhen(true, nameof(Variant))]
    public bool IsFound => Status == TranscodeVariantStatus.Found && Variant is not null;

    public static TranscodeVariantResult Of(TranscodeVariantStatus status)
    {
        return new TranscodeVariantResult(status, null);
    }
}
