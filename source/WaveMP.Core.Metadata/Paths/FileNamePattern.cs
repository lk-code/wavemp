namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// A pattern shared by the file names of a folder, e.g. <c>{TrackNumber} - {Artist} - {Title}</c>, found by
/// <see cref="FileNamePatternDetector"/>.
/// </summary>
public sealed class FileNamePattern
{
    internal FileNamePattern(bool hasNumber, IReadOnlyList<FileNamePart> textParts)
    {
        HasNumber = hasNumber;
        TextParts = textParts;
    }

    /// <summary>
    /// The names start with the track number.
    /// </summary>
    public bool HasNumber { get; }

    /// <summary>
    /// Meaning of the texts between the separators, in their order.
    /// </summary>
    public IReadOnlyList<FileNamePart> TextParts { get; }

    /// <summary>
    /// Reads a name with this pattern. Returns <c>null</c> if the name does not have its shape.
    /// </summary>
    public FileNameFields? Match(string baseName)
    {
        (string? number, string rest) = HasNumber ? FileNameTokens.SplitNumber(baseName) : (null, baseName);
        if (HasNumber && number is null)
        {
            return null;
        }

        string[] texts = FileNameTokens.SplitTexts(rest);
        if (texts.Length != TextParts.Count)
        {
            return null;
        }

        string? Text(FileNamePart part)
        {
            int index = TextParts.ToList().IndexOf(part);
            return index < 0 ? null : texts[index];
        }

        return new FileNameFields(
            TrackNumber: FileNameTokens.ParseNumber(number),
            Artist: Text(FileNamePart.Artist),
            Album: Text(FileNamePart.Album),
            Title: Text(FileNamePart.Title));
    }

    public override string ToString()
    {
        IEnumerable<FileNamePart> parts = HasNumber ? [FileNamePart.TrackNumber, .. TextParts] : TextParts;
        return string.Join(" - ", parts.Select(part => "{" + part + "}"));
    }
}
