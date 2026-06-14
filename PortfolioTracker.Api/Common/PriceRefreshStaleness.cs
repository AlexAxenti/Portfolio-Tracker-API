namespace PortfolioTracker.Api.Common;

public static class PriceRefreshStaleness
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(1);

    public static bool IsStale(DateTime? priceLastUpdatedAt, DateTime nowUtc)
    {
        return priceLastUpdatedAt is null || priceLastUpdatedAt.Value < nowUtc.Subtract(Ttl);
    }
}
