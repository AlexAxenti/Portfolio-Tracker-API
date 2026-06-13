namespace PortfolioTracker.Api.DTOs.Messaging;

public sealed record PriceRefreshRequestedMessage(
    Guid UserId,
    IReadOnlyList<PriceRefreshTickerMessage> Tickers,
    DateTime RequestedAtUtc
);

public sealed record PriceRefreshTickerMessage(
    Guid TickerId,
    string Symbol
);