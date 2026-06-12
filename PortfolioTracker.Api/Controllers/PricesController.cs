using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Services;

namespace PortfolioTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PricesController(IPricesService pricesService) : ControllerBase
{
    [HttpPost("refresh-prices")]
    public async Task<ActionResult<IReadOnlyList<HoldingDto>>> RefreshPrices()
    {
        var holdings = await pricesService.RefreshPricesAsync();
        return Ok(holdings);
    }
}
