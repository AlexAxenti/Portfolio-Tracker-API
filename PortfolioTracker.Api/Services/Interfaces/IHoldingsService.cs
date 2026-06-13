using PortfolioTracker.Api.DTOs.Holdings;

namespace PortfolioTracker.Api.Services;

public interface IHoldingsService
{
    Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync();

    Task<HoldingDto?> GetHoldingAsync(Guid id);

    Task<HoldingDto> CreateHoldingAsync(CreateHoldingRequest request);

    Task<HoldingDto?> UpdateHoldingAsync(Guid id, UpdateHoldingRequest request);

    Task<bool> DeleteHoldingAsync(Guid id);
}
