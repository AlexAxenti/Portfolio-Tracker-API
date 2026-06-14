using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.Api.DTOs.Prices;
using PortfolioTracker.Api.Services;
using PortfolioTracker.Api.Services.Messaging;

namespace PortfolioTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PricesController(IPricesService pricesService) : ControllerBase
{
    [HttpPost("refresh-prices")]
    public async Task<ActionResult<PriceRefreshQueuedResponse>> RefreshPrices()
    {
        try
        {
            var response = await pricesService.RefreshPricesAsync();
            return Accepted(response);
        }
        catch (MessageBrokerUnavailableException ex)
        {
            return Problem(
                title: "Price refresh is temporarily unavailable.",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
