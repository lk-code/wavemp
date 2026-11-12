using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Transcoding.Services;

/// <summary>
/// A running transcode with its track.
/// </summary>
/// <param name="Job">The transcode.</param>
/// <param name="Track">The track; <c>null</c> if it was removed in the meantime.</param>
public sealed record TranscodeJobItem(TranscodeJobInfo Job, TrackRow? Track);
