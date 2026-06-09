using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.DTOs.Trades;

public sealed record UpdateTradeRequest(
    string Ticker,
    TradeType Type,
    decimal Quantity,
    decimal Price,
    DateTime TradeDate,
    string? Notes);
