namespace PortfolioTracker.Api.DTOs.Messaging;

public sealed record PriceRefreshRequestedMessage(
    Guid UserId,
    Guid TickerId,
    string Ticker,
    DateTime RequestedAtUtc
);
