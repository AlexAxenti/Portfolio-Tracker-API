using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Services;

public interface IHoldingsService
{
    Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync();

    Task<HoldingDto?> GetHoldingAsync(Guid id);

    Task<HoldingDto> CreateHoldingAsync(CreateHoldingRequest request);

    Task<HoldingDto?> UpdateHoldingAsync(Guid id, UpdateHoldingRequest request);

    Task<bool> DeleteHoldingAsync(Guid id);

    Task ApplyTradeAsync(TradeEntity trade);

    Task ReverseTradeAsync(TradeEntity trade);
}
