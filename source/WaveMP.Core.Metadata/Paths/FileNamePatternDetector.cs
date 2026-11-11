using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// Finds a pattern that the file names of a folder share, e.g. in a media folder where all files lie in the root:
/// <c>A - X</c>, <c>A - Y</c>, <c>B - Z</c> are <c>{Artist} - {Title}</c>. A pattern needs at least
/// <see cref="MinNames"/> names, and <see cref="MinShare"/> of them must have the same shape.
/// </summary>
public static class FileNamePatternDetector
{
    public const int MinNames = 3;
    public const double MinShare = 0.8;

    /// <summary>
    /// A text position whose values repeat this much (distinct values per name) is the artist.
    /// </summary>
    private const double MaxArtistVariety = 0.5;

    public static FileNamePattern? Detect(IReadOnlyCollection<string> baseNames)
    {
        if (baseNames.Count < MinNames)
        {
            return null;
        }

        Shape[] shapes = [.. baseNames.Select(ShapeOf)];
        IGrouping<(bool HasNumber, int Texts), Shape>? common = shapes
            .GroupBy(shape => (shape.HasNumber, shape.Texts.Length))
            .OrderByDescending(group => group.Count())
            .First();
        if (common.Count() < baseNames.Count * MinShare || common.Key.Texts is < 1 or > 3)
        {
            return null;
        }

        FileNamePart[] parts = common.Key.Texts switch
        {
            1 => [FileNamePart.Title],
            2 => TwoTextParts([.. common]),
            _ => [FileNamePart.Artist, FileNamePart.Album, FileNamePart.Title],
        };

        return new FileNamePattern(common.Key.HasNumber, parts);
    }

    /// <summary>
    /// Of two texts, the one that repeats (few distinct values) is the artist; without a clear difference the first.
    /// </summary>
    private static FileNamePart[] TwoTextParts(IReadOnlyList<Shape> shapes)
    {
        double first = Variety(shapes, 0);
        double second = Variety(shapes, 1);
        return second <= MaxArtistVariety && second < first
            ? [FileNamePart.Title, FileNamePart.Artist]
            : [FileNamePart.Artist, FileNamePart.Title];
    }

    private static double Variety(IReadOnlyList<Shape> shapes, int position)
    {
        int distinct = shapes.Select(shape => MetadataText.ToKey(shape.Texts[position])).Distinct().Count();
        return (double)distinct / shapes.Count;
    }

    private static Shape ShapeOf(string baseName)
    {
        (string? number, string rest) = FileNameTokens.SplitNumber(baseName);
        return new Shape(number is not null, FileNameTokens.SplitTexts(rest));
    }

    private sealed record Shape(bool HasNumber, string[] Texts);
}
