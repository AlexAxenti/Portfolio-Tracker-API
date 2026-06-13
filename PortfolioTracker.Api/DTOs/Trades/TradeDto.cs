using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.DTOs.Trades;

public sealed record TradeDto(
    Guid Id,
    Guid UserId,
    Guid TickerId,
    string Ticker,
    TradeType Type,
    decimal Quantity,
    decimal Price,
    DateTime TradeDate,
    string? Notes,
    DateTime CreatedAt);
