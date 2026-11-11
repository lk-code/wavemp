using WaveMP.Core.Library.Covers;

namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// A cover to deliver, or why there is none.
/// </summary>
public sealed record CoverDeliveryResult(CoverStatus Status, CoverDelivery? Delivery)
{
    public static CoverDeliveryResult OwnerNotFound { get; } = new(CoverStatus.OwnerNotFound, null);

    public static CoverDeliveryResult NotFound { get; } = new(CoverStatus.NotFound, null);

    public static CoverDeliveryResult Found(CoverDelivery delivery)
    {
        return new CoverDeliveryResult(CoverStatus.Found, delivery);
    }
}
