namespace PortfolioTracker.Api.DTOs.Prices;

public sealed record PriceRefreshQueuedResponse(
    string Message,
    IReadOnlyList<string> QueuedTickers,
    DateTime QueuedAtUtc
);
