using PortfolioTracker.Api.DTOs.Holdings;

namespace PortfolioTracker.Api.Services;

public interface IPricesService
{
    Task<IReadOnlyList<HoldingDto>> RefreshPricesAsync();
}
