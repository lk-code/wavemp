namespace WaveMP.Core.Library.Covers;

/// <summary>
/// A cover that was found, or why none was found.
/// </summary>
public sealed record CoverResult(CoverStatus Status, CoverImage? Image)
{
    public static CoverResult OwnerNotFound { get; } = new(CoverStatus.OwnerNotFound, null);

    public static CoverResult NotFound { get; } = new(CoverStatus.NotFound, null);

    public static CoverResult Found(CoverImage image)
    {
        return new CoverResult(CoverStatus.Found, image);
    }
}
