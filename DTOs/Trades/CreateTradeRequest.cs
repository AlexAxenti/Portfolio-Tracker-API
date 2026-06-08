using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.DTOs.Trades;

public sealed record CreateTradeRequest(
    string Ticker,
    TradeType Type,
    decimal Quantity,
    decimal Price,
    DateTime TradeDate,
    string? Notes);
