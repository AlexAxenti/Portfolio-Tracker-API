using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.Api.DTOs.Prices;
using PortfolioTracker.Api.Services;

namespace PortfolioTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PricesController(IPricesService pricesService) : ControllerBase
{
    [HttpPost("refresh-prices")]
    public async Task<ActionResult<PriceRefreshQueuedResponse>> RefreshPrices()
    {
        var response = await pricesService.RefreshPricesAsync();
        return Accepted(response);
    }
}
