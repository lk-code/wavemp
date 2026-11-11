namespace WaveMP.Core.Scanning.Writers;

/// <summary>
/// Versions of what the scan stores. Raising <see cref="Metadata"/> makes the next scan read every file again, e.g.
/// when a later version stores more of a file.
/// </summary>
public static class ScannerVersions
{
    public const int Metadata = 1;
}
