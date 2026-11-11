namespace WaveMP.Data.Configurations;

/// <summary>
/// Column sizes shared by the library tables.
/// </summary>
internal static class LibraryColumns
{
    /// <summary>
    /// Names and titles from tags or file names; longer texts are cut by the scan.
    /// </summary>
    public const int TextMaxLength = 512;

    public const int GenreMaxLength = 128;
    public const int EnumMaxLength = 32;
    public const int ExtensionMaxLength = 16;
    public const int TechnicalMaxLength = 64;
    public const int AlbumKeyLength = 64;

    /// <summary>
    /// Index method of the trigram indexes (pg_trgm). GIN indexes speed up word similarity (<c>&lt;%</c>) and
    /// <c>ILIKE '%...%'</c>.
    /// </summary>
    public const string TrigramIndexMethod = "gin";

    public const string TrigramOperators = "gin_trgm_ops";
}
