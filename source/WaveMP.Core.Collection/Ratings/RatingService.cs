using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.Ratings;

internal sealed class RatingService(ITrackRatingRepository ratings, TimeProvider timeProvider) : IRatingService
{
    public async Task<RatingStatus> SetAsync(
        Guid userId,
        Guid trackId,
        int rating,
        CancellationToken cancellationToken = default)
    {
        if (!TrackRatings.IsValid(rating))
        {
            return RatingStatus.Invalid;
        }

        bool found = rating == TrackRatings.Min
            ? await ratings.RemoveAsync(userId, trackId, cancellationToken)
            : await ratings.SetAsync(
                userId,
                trackId,
                rating,
                TruncateToMicroseconds(timeProvider.GetUtcNow()),
                cancellationToken);
        return found ? RatingStatus.Done : RatingStatus.NotFound;
    }

    /// <summary>
    /// PostgreSQL keeps microseconds; times are created with the precision they are read with later.
    /// </summary>
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);
    }
}
