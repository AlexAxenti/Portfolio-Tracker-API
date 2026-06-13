using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Services;

public interface IHoldingsTradeService
{
    Task ApplyTradeAsync(TradeEntity trade);

    Task ReverseTradeAsync(TradeEntity trade);
}
