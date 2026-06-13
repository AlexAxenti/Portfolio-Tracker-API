using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Services;

public interface ITickersService
{
    Task<TickerEntity?> GetTickerAsync(Guid id);

    Task<TickerEntity?> GetTickerAsync(string symbol);

    Task<TickerEntity> CreateTickerAsync(string symbol, decimal? initialCurrentPrice = null);

    Task<TickerEntity> GetOrCreateTickerAsync(string symbol);
}
