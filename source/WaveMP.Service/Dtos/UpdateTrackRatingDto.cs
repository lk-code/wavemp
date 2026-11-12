using System.ComponentModel.DataAnnotations;
using WaveMP.Core.Collection.Ratings;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Rates a track for the current profile.
/// </summary>
public sealed class UpdateTrackRatingDto
{
    /// <summary>
    /// Number of stars from 1 to 5; 0 removes the rating.
    /// </summary>
    [Required]
    [Range(TrackRatings.Min, TrackRatings.Max)]
    public required int? Rating { get; init; }
}
