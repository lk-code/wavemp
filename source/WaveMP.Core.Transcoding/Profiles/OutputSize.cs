namespace WaveMP.Core.Transcoding.Profiles;

/// <summary>
/// Size of a transcoded picture: never larger than the source, at most the largest height, keeping the aspect ratio,
/// with even sides (which H.264 with 4:2:0 needs).
/// </summary>
public sealed record OutputSize(int Width, int Height)
{
    /// <summary>
    /// The size for a source; <c>null</c> if the size of the source is unknown.
    /// </summary>
    public static OutputSize? Of(int? width, int? height, int maxHeight)
    {
        if (width is not > 0 || height is not > 0 || maxHeight <= 0)
        {
            return null;
        }

        int sourceWidth = width.Value;
        int sourceHeight = height.Value;
        if (sourceHeight <= maxHeight)
        {
            return new OutputSize(Even(sourceWidth), Even(sourceHeight));
        }

        double scaled = sourceWidth * (double)maxHeight / sourceHeight / 2;
        int scaledWidth = (int)Math.Round(scaled, MidpointRounding.AwayFromZero) * 2;
        return new OutputSize(Math.Max(2, scaledWidth), Even(maxHeight));
    }

    private static int Even(int value)
    {
        return Math.Max(2, value & ~1);
    }
}
