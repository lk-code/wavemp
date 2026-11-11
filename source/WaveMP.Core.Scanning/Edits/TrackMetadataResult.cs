using System.Diagnostics.CodeAnalysis;

namespace WaveMP.Core.Scanning.Edits;

/// <summary>
/// Result of writing metadata: the metadata of the track read again, or why nothing was written.
/// </summary>
public sealed record TrackMetadataResult(TrackMetadata? Metadata, TrackMetadataError? Error)
{
    [MemberNotNullWhen(true, nameof(Metadata))]
    public bool IsSuccess => Metadata is not null;

    public static TrackMetadataResult Success(TrackMetadata metadata)
    {
        return new TrackMetadataResult(metadata, null);
    }

    public static TrackMetadataResult Failure(TrackMetadataError error)
    {
        return new TrackMetadataResult(null, error);
    }
}
