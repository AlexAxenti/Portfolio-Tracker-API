using PortfolioTracker.Api.DTOs.Trades;

namespace PortfolioTracker.Api.Services;

public interface ITradesService
{
    Task<IReadOnlyList<TradeDto>> GetTradesAsync();

    Task<TradeDto> CreateTradeAsync(CreateTradeRequest request);

    Task<TradeDto?> UpdateTradeAsync(Guid id, UpdateTradeRequest request);

    Task<bool> DeleteTradeAsync(Guid id);
}
