using PortfolioTracker.Api.DTOs.Prices;

namespace PortfolioTracker.Api.Services;

public interface IPricesService
{
    Task<PriceRefreshQueuedResponse> RefreshPricesAsync();
}
