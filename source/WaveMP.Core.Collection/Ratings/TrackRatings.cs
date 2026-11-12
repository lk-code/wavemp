namespace WaveMP.Core.Collection.Ratings;

/// <summary>
/// The rating of a track: 0 (not rated) to 5 stars.
/// </summary>
public static class TrackRatings
{
    /// <summary>
    /// Not rated; setting it removes the rating.
    /// </summary>
    public const int Min = 0;

    public const int Max = 5;

    public static bool IsValid(int rating)
    {
        return rating is >= Min and <= Max;
    }
}
