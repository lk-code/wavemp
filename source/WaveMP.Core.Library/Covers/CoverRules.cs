using WaveMP.Data.Entities;

namespace WaveMP.Core.Library.Covers;

/// <summary>
/// Which cover rule applies: the most specific level that sets one wins (track, then album, then the global setting).
/// Albums and tracks do not carry their own rule yet; they inherit the global one (<c>null</c>).
/// </summary>
public static class CoverRules
{
    public static CoverPriority Effective(CoverPriority global, CoverPriority? album, CoverPriority? track)
    {
        return track ?? album ?? global;
    }
}
